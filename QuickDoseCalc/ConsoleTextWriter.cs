using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace AutoPlanning
{
    internal class ConsoleTextWriter : System.IO.TextWriter
    {
        TextBox textBox;
        delegate void WriteFunc(string value);
        WriteFunc write;
        WriteFunc writeLine;

        public ConsoleTextWriter(TextBox textBox)
        {
            this.textBox = textBox;
            write = Write;
            writeLine = WriteLine;
        }

        ///// <summary>
        ///// code to -UTF8
        ///// </summary>
        public override Encoding Encoding
        {
            get { return Encoding.UTF8; }
            //get { return Encoding.Unicode; }
        }

        ///// <summary>
        ///// override
        ///// </summary>
        public override void Write(string value)
        {
            if (!textBox.Dispatcher.CheckAccess())
            {
                textBox.Dispatcher.BeginInvoke(write, value);
            }
            else
            {
                textBox.AppendText(value);
            }
            textBox.ScrollToEnd();
            DispatcherHelper.DoEvents();
        }

        ///// <summary>
        ///// override
        ///// </summary>
        public override void WriteLine(string value)
        {
            if (!textBox.Dispatcher.CheckAccess())
            {
                textBox.Dispatcher.BeginInvoke(write, value);
            }
            else
            {
                textBox.AppendText(value);
                textBox.AppendText(this.NewLine);
            }
            textBox.ScrollToEnd();
            DispatcherHelper.DoEvents();
        }
    }
    public static class DispatcherHelper
    {
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public static void DoEvents()
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(ExitFrames), frame);
            try { Dispatcher.PushFrame(frame); }
            catch (InvalidOperationException) { }
        }
        private static object ExitFrames(object frame)
        {
            ((DispatcherFrame)frame).Continue = false;
            return null;
        }
    }
}
