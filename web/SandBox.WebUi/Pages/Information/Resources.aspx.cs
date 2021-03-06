﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using SandBox.Connection;
using SandBox.Db;
using SandBox.Log;
using SandBox.WebUi.Base;
using DevExpress.Web.ASPxEditors;

namespace SandBox.WebUi.Pages.Information
{
    public partial class Resources : BaseMainPage
    {
        protected new void Page_Load(object sender, EventArgs e)
        {
            base.Page_Load(sender, e);
            PageTitle = "Ресурсы";
            PageMenu = "~/App_Data/SideMenu/Information/InformationMenu.xml";

            if (!IsPostBack)
            {
                gridViewMachines.KeyFieldName = "Id";
                gridViewMachines.Visible = false;
                labelNoItems.Text = "";
                linkRegisterNewVm.Visible = false;

                UpdateTableView();
                DbManager.OnTableUpdated += VmManager_OnTableUpdated;

                //foreach (var machine in VmManager.GetVms())
                //{
                //    if(machine.State != (int)VmManager.State.UNAVAILABLE)
                //    GetVmStatus(machine.Id);
                //}

                if (IsUserInRole("Administrator"))
                {
                    linkRegisterNewVm.Visible = true;
                }
            }

            UpdateTableView();
        }

        public String LirInfo()
        {
            IQueryable<Vm> vms = IsUserInRole("Administrator") ? VmManager.GetVms() : VmManager.GetVms(UserId);

            Int32 freeVms = 0;
            Int32 usedVms = 0;

            foreach (var vm in vms)
            {
                if (vm.State == (Int32)VmManager.State.STARTED) usedVms++;
                if (vm.State == (Int32)VmManager.State.STOPPED) freeVms++;
            }

            String txt = "Используется " + usedVms + " ЛИР, " + freeVms + " ЛИР свободно";

            if ((usedVms == 0) && (freeVms == 0))
            {
                txt = "Информации по ЛИР недоступна";
            }
            else if (usedVms == 0)
            {
                txt = "Свободно " + freeVms + " ЛИР";
            }
            else if (freeVms == 0)
            {
                txt = "Используется " + usedVms + " ЛИР, свободных ЛИР нет";
            }

            return txt;
        }

        private void VmManager_OnTableUpdated(Table table)
        {
            Debug.Print("OnTableUpdated!!!");
            //UpdateTableView();
        }

        private void UpdateTableView()
        {
            Int32 count = IsUserInRole("Administrator") ? VmManager.GetVmsTableViewCount() : VmManager.GetVmsTableViewCount(UserId);
            IQueryable vms = IsUserInRole("Administrator") ? VmManager.GetVmsTableView() : VmManager.GetVmsTableView(UserId);

            if (count > 0)
            {
                gridViewMachines.Visible = true;
                labelNoItems.Text = "";

                gridViewMachines.DataSource = vms;
                gridViewMachines.DataBind();
            }

            if (count == 0)
            {
                gridViewMachines.Visible = false;
                labelNoItems.Text = "У вас нет ВЛИР, доступных для использования";
            }
        }

        protected void UpdateTimerTick(object sender, EventArgs e)
        {
            //UpdateTableView();
        }

        protected void UpdateInfoTimerTick(object sender, EventArgs e)
        {
            IQueryable<Vm> vms = IsUserInRole("Administrator") ? VmManager.GetVms() : VmManager.GetVms(UserId);

            Int32 freeVms = 0;
            Int32 usedVms = 0;

            foreach (var vm in vms)
            {
                if (vm.State == (Int32)VmManager.State.STARTED) usedVms++;
                if (vm.State == (Int32)VmManager.State.STOPPED) freeVms++;
            }

            String txt = "Используется " + usedVms + " ЛИР, " + freeVms + " ЛИР свободно";

            if ((usedVms == 0) && (freeVms == 0))
            {
                txt = "Информации по ЛИР недоступна";
            }
            else if (usedVms == 0)
            {
                txt = "Свободно " + freeVms + " ЛИР";
            }
            else if (freeVms == 0)
            {
                txt = "Используется " + usedVms + " ЛИР, свободных ЛИР нет";
            }

            //labelVmInfo.Text = txt;
        }


        private void GetVmStatus(Int32 id)
        {
            String machineName = VmManager.GetVmName(id);
            MLogger.LogTo(Level.TRACE, false, "Get status for " + machineName);

            VmManager.UpdateVmState(machineName, (Int32)VmManager.State.UPDATING);

            Packet packet = new Packet { Type = PacketType.CMD_VM_STATUS, Direction = PacketDirection.REQUEST };
            packet.AddParameter(Encoding.UTF8.GetBytes(machineName));
            SendPacket(packet.ToByteArray());
        }

        public static void StartVm(Int32 id)
        {
            String machineName = VmManager.GetVmName(id);
            Debug.Print("Start vm: " + machineName);
            Packet packet = new Packet { Type = PacketType.CMD_VM_START, Direction = PacketDirection.REQUEST };
            packet.AddParameter(Encoding.UTF8.GetBytes(machineName));
            SendPacket(packet.ToByteArray());
        }

        
        private static void TryDeleteVm(Int32 id)
        {
             String machineName = VmManager.GetVmName(id);
            Packet packet = new Packet { Type = PacketType.CMD_VM_DELETE, Direction = PacketDirection.REQUEST };
            packet.AddParameter(Encoding.UTF8.GetBytes(machineName));
            SendPacket(packet.ToByteArray());

            VmManager.DeleteVm(id);
        }
        private void DeleteVm(Int32 id)
        {
            TryDeleteVm(id);
            UpdateTableView();
        }

        public static void StopVm(Int32 id)
        {
            Vm vm = VmManager.GetVm(id);

            if (vm != null)
            {
                Packet packet = new Packet { Type = PacketType.CMD_VM_STOP, Direction = PacketDirection.REQUEST };
                packet.AddParameter(Encoding.UTF8.GetBytes(vm.Name));
                SendPacket(packet.ToByteArray());

                if (vm.EnvType == 0)
                    TryDeleteVm(vm.Id);
            }

        }

        protected void GridViewMachinesHtmlRowPrepared(object sender, DevExpress.Web.ASPxGridView.ASPxGridViewTableRowEventArgs e)
        {
            if (e.RowType != DevExpress.Web.ASPxGridView.GridViewRowType.Data) return;
            ASPxHyperLink linkSession = gridViewMachines.FindRowCellTemplateControl(e.VisibleIndex, null, "HLSession") as ASPxHyperLink;
            ASPxHyperLink linkMlwr = gridViewMachines.FindRowCellTemplateControl(e.VisibleIndex, null, "HLMlwr") as ASPxHyperLink;
            if (linkSession != null && linkMlwr != null)
            {
                linkMlwr.Visible = false;
                linkSession.Visible = false;
            }
            Int32 vmId = (Int32)e.KeyValue;
            Vm vm = VmManager.GetVm(vmId);
            var rsch = ResearchManager.GetRunnigResearchByVmID(vmId);

            switch (vm.State)
            {
                case (Int32)VmManager.State.ERROR:
                    {
                        e.Row.BackColor = Color.FromArgb(0xF0, 0x7E, 0x41);
                        break;
                    }
                case (Int32)VmManager.State.STARTED:
                    {
                        e.Row.BackColor = Color.FromArgb(0xDB, 0xFA, 0xA5);
                        if (linkSession != null && linkMlwr != null)
                        {
                            if (rsch != null)
                            {
                                linkMlwr.NavigateUrl += "?mlwrID=" + rsch.MlwrId;
                                linkSession.NavigateUrl += "?research=" + rsch.Id;
                                linkSession.Text = rsch.ResearchName;
                                linkMlwr.Text = MlwrManager.GetMlwr(rsch.MlwrId).Name;
                                linkSession.Visible = true;
                                linkMlwr.Visible = true;
                            }
                        }
                        break;
                    }
                case (Int32)VmManager.State.STARTING:
                    {
                        e.Row.BackColor = Color.FromArgb(0xE3, 0xE3, 0xDC);
                        break;
                    }
                case (Int32)VmManager.State.STOPPED:
                    {
                        e.Row.BackColor = Color.FromArgb(0xF2, 0xEF, 0x8A);
                        break;
                    }
                case (Int32)VmManager.State.STOPPING:
                    {
                        e.Row.BackColor = Color.FromArgb(0xE3, 0xE3, 0xDC);
                        break;
                    }
                case (Int32)VmManager.State.UPDATING:
                    {
                        e.Row.BackColor = Color.FromArgb(0xE3, 0xE3, 0xDC);
                        break;
                    }
            }
        }

        protected void GridViewMachinesCustomButtonCallback(object sender, DevExpress.Web.ASPxGridView.ASPxGridViewCustomButtonCallbackEventArgs e)
        {
            Int32 id = Convert.ToInt32(gridViewMachines.GetRowValues(e.VisibleIndex, "Id"));
            switch (e.ButtonID)
            {
                case "runButton":
                    Debug.Print("run: " + id);
                    StartVm(id);
                    break;
                case "stopButton":
                    Debug.Print("stop: " + id);
                    StopVm(id);
                    break;
                case "deleteButton":
                    Debug.Print("delete: " + id);
                    DeleteVm(id);
                    break;
                case "editButton":
                    Debug.Print("edit: " + id);
                    break;
            }
        }

        protected void Timer1_Tick(object sender, EventArgs e)
        {
            
        }

        private string GetCpuInfoFromString(string p, int i)
        {
            return String.Format("CPU{0}:{1}%",i, p);
        }

        protected void Timer2_Tick(object sender, EventArgs e)
        {
            UpdateTableView();
        }
    }//end class
}//end namespace