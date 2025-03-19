using AutoPlanning;
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
            //CancellationTokenSource cts = new CancellationTokenSource();
            //OpenWindowGetter.LaunchWindowsClosingThread(cts.Token);
            Console.SetOut(new ConsoleTextWriter(textBox_Console));
            Console.WriteLine(" ");
            Console.WriteLine($"Course:{context.Course.Id}");
            Console.WriteLine($"Patient:{context.Patient.Id}");
            getTxPlans();
        }



        private void CalcPlanHasNoDose()
        {
            progressBar1.Value = 0;
            for (int i = 0; i < context.Course.IonPlanSetups.Count(); i++) 
            {
                var plan = context.Course.IonPlanSetups.ToList()[i];
                if (plan.IsDoseValid == false || checkBox_ForceReCalc.IsChecked==true)
                {
                    Console.WriteLine($"Calc {i+1}/{context.Course.IonPlanSetups.Count()} [{plan.Id}] start");
                    DoseCalc(plan);
                    Console.WriteLine($"Calc {i + 1}/{context.Course.IonPlanSetups.Count()} [{plan.Id}] finish");
                }
                else
                {
                    Console.WriteLine(string.Format("{0} has dose, ignored!", plan.Id));
                }
                progressBar1.Value = (i + 1) * 100 / context.Course.IonPlanSetups.Count();
            }
            progressBar1.Value = 100;
            Console.WriteLine(string.Format("All tasks finished!"));
            MessageBox.Show("All tasks finished!");
            progressBar1.Value = 0;
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
            DoseValue doseValue = new DoseValue(Convert.ToDouble(textBox_RefFxDose.Text), refPlan.DosePerFraction.Unit);
            plan.SetPrescription(Convert.ToInt32(refPlan.NumberOfFractions), doseValue, refPlan.TreatmentPercentage);
            Console.WriteLine($"set Fx Prescription as [{refPlan.DosePerFraction}]");
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
            UpdateFxDose();
        }

        private void button_Start_Click(object sender, RoutedEventArgs e)
        {
            if (checkBox_AutoCloseWarning.IsChecked == true)
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                OpenWindowGetter.LaunchWindowsClosingThread(cts.Token);
            }
            context.Patient.BeginModifications();
            CalcPlanHasNoDose();
        }

        private void comboBox_SelectPlan_DropDownClosed(object sender, EventArgs e)
        {
            UpdateFxDose();
        }

        private void UpdateFxDose()
        {
            refPlan = context.Course.IonPlanSetups.ToList().Where(x => x.Id == comboBox_SelectPlan.Text).FirstOrDefault();
            if (refPlan != null)
            {
                textBox_RefFxDose.Text = refPlan.DosePerFraction.ValueAsString;
            }
            else { Console.WriteLine("Select Null, please check!"); }
        }

    }
}
