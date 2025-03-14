﻿using AutoPlanning;
using SmartPlanning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace QuickDoseCalc
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MyWindow : Window
    {
        private ScriptContext context;
        private IonPlanSetup refPlan;
        public MyWindow(ScriptContext context_)
        {
            context = context_;
            InitializeComponent();
            // close popup window
            CancellationTokenSource cts = new CancellationTokenSource();
            OpenWindowGetter.LaunchWindowsClosingThread(cts.Token);
            Console.SetOut(new ConsoleTextWriter(textBox_Console));
            Console.WriteLine(" ");
            Console.WriteLine($"Course:{context.Course.Id}");
            Console.WriteLine($"Patient:{context.Patient.Id}");
            getTxPlans();
        }



        private void CalcPlanHasNoDose()
        {
            for (int i = 0; i < context.Course.IonPlanSetups.Count(); i++) 
            {
                var plan = context.Course.IonPlanSetups.ToList()[i];
                if (plan.IsDoseValid == false || checkBox_ForceReCalc.IsChecked==true)
                {
                    Console.WriteLine(string.Format("Start Calc {0}", plan.Id));
                    DoseCalc(plan);
                    Console.WriteLine(string.Format("Finished Calc {0}", plan.Id));
                }
                else
                {
                    Console.WriteLine(string.Format("{0} has dose, ignored!", plan.Id));
                }
            }
            Console.WriteLine(string.Format("All tasks finished!"));
        }

        private void DoseCalc(IonPlanSetup plan)
        {
            //if (plan.Series != null)
            //{
            //    //plan.Series.SetImagingDevice(refPlan.Series.ImagingDeviceId);
            //    //Console.WriteLine($"set ImagingDeviceId from {plan.Series.ImagingDeviceId} to {refPlan.Series.ImagingDeviceId}");
            //    //Console.WriteLine($"set ImagingDeviceManufacturer from {plan.Series.ImagingDeviceManufacturer} to {refPlan.Series.ImagingDeviceManufacturer}");
            //    //Console.WriteLine($"set ImagingDeviceModel from {plan.Series.ImagingDeviceModel} to {refPlan.Series.ImagingDeviceModel}");
            //    //Console.WriteLine($"set ImagingDeviceSerialNo from {plan.Series.ImagingDeviceSerialNo} to {refPlan.Series.ImagingDeviceSerialNo}");
            //}
            plan.SetPrescription(Convert.ToInt32(refPlan.NumberOfFractions), refPlan.DosePerFraction, refPlan.TreatmentPercentage);
            plan.SetCalculationModel(CalculationType.ProtonVolumeDose, refPlan.ProtonCalculationModel);
            foreach (var item in refPlan.ProtonCalculationOptions)
            {
                plan.SetCalculationOption(refPlan.ProtonCalculationModel, item.Key, item.Value);
            }
            var res1 = plan.CalculateDoseWithoutPostProcessing();
            if(res1.Success == true)
            {
                Console.WriteLine("Calculation success!");
            }
            else
            {
                Console.WriteLine($"Calculation failed!");
            }
        }

        private void getTxPlans()
        {
            comboBox_SelectPlan.Items.Clear();
            List<string> txPlans = new List<string>();
            // Ensure IonPlanSetups is not null or empty
            if (context.Course.IonPlanSetups != null && context.Course.IonPlanSetups.Any())
            {
                foreach (var item in context.Course.IonPlanSetups)
                {
                    // Ensure item.Id is not null before adding
                    if (item.Id != null)
                    {
                        txPlans.Add(item.Id.ToString());
                        //Console.WriteLine($"Adding {item.Id}");
                    }
                }

                // Bind the list to the ComboBox
                comboBox_SelectPlan.ItemsSource = txPlans;

                // Only set SelectedIndex if there are items
                if (txPlans.Count > 0)
                {
                    comboBox_SelectPlan.SelectedIndex = 0;
                }
            }
            else
            {
                Console.WriteLine("No plan setups found.");
            }
        }

        private void button_Start_Click(object sender, RoutedEventArgs e)
        {
            context.Patient.BeginModifications();
            CalcPlanHasNoDose();
        }

        private void comboBox_SelectPlan_DropDownClosed(object sender, EventArgs e)
        {
            refPlan = context.Course.IonPlanSetups.ToList().Where(x => x.Id == comboBox_SelectPlan.Text).FirstOrDefault();
            if (refPlan != null)
            {
                label_FxDoseValue.Content = ":" + refPlan.DosePerFraction.ValueAsString;
            }
            else { Console.WriteLine("Select Null, please check!"); }
        }
    }
}
