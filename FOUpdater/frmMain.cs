using System.Net;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using FOUpdater;

namespace FOUpdater
{
    public partial class frmMain : Form
    {
        private object winformLocker = new object();
        private FOUpdaterClient updater = null;
        private string DefaultSource = null;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams result = base.CreateParams;
                if( Environment.OSVersion.Platform == PlatformID.Win32NT
                    && Environment.OSVersion.Version.Major >= 6 )
                {
                    result.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
                }

                return result;
            }
        }

        public frmMain( string defaultSource = null )
        {
            InitializeComponent();

            this.labelGeneral.Text = labelDetailed.Text = "";
            this.progressAll.Value = progressFile.Value = 0;

            this.buttonExit.Enabled = false;

            if( defaultSource != null && defaultSource.Length > 0 )
                this.DefaultSource = defaultSource;
        }

        void winformMessage( FOUpdaterMessage type, string message )
        {
            lock( this.winformLocker )
            {
                Label label = null;
                switch( type )
                {
                    case FOUpdaterMessage.General:
                        label = this.labelGeneral;
                        break;
                    case FOUpdaterMessage.Detailed:
                        label = this.labelDetailed;
                        break;
                    case FOUpdaterMessage.Info:
                        MessageBox.Show( message,
                            "FOUpdater",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information );
                        return;
                    default:
                        throw new NotImplementedException( Enum.GetName( typeof( FOUpdaterMessage ), type ) );
                }
                label.SetText( message );
            }
        }

        void winformProgress( FOUpdaterProgress type, uint value )
        {
            lock( this.winformLocker )
            {
                switch( type )
                {
                    case FOUpdaterProgress.FileValue:
                        if( value > this.progressFile.Maximum )
                            value = (uint)this.progressFile.Maximum;
                        this.progressFile.SetValue( (int)value );
                        break;
                    case FOUpdaterProgress.FileMaximum:
                        if( this.progressFile.Value > (int)value )
                            this.progressFile.Value = (int)value;
                        this.progressFile.SetMaximum( (int)value );
                        break;
                    case FOUpdaterProgress.TotalValue:
                        if( value > this.progressAll.Maximum )
                            value = (uint)this.progressAll.Maximum;
                        this.progressAll.SetValue( (int)value );
                        break;
                    case FOUpdaterProgress.TotalMaximum:
                        this.progressAll.SetMaximum( (int)value );
                        break;
                    default:
                        throw new NotImplementedException( Enum.GetName( typeof( FOUpdaterProgress ), type ) );
                }
            }
        }

        #region Events

        private void backgroundWorker_DoWork( object sender, DoWorkEventArgs e )
        {
            this.CancelButton = this.buttonCancel;

            updater = new FOUpdaterClient();
            FOUpdaterResult result = updater.Update( winformMessage, winformProgress, DefaultSource );

            this.AcceptButton = this.buttonCancel;
            this.CancelButton = this.buttonExit;

            if( result == FOUpdaterResult.OK || result == FOUpdaterResult.Cancel )
            {
                string run = null;
                if( updater.FOnlineCfg != null )
                    run = updater.FOnlineCfg.GetValue( FOUpdaterClient.Strings.Section, FOUpdaterClient.Strings.Run, (string)null );

                if( run != null && run.Length > 0 )
                {
                    string[] runOption = run.Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries );
                    if( runOption.Length == 2 &&
                        File.Exists( runOption[0] ) &&
                        runOption[1].Length > 0 )
                    {
                        buttonCancel.SetEnabled( true );
                        buttonCancel.SetText( runOption[1] );
                        buttonCancel.SetTag( new string[] { runOption[0], null } );
                    }
                }
                else // FOConfigEx support
                {
                    string[] program = { "FOnline", "FOnlineGL" };

                    if( updater.FOnlineCfg != null )
                        run = updater.FOnlineCfg.GetValue( "FOConfig", "PlayProgram", (string)null );
                    byte tmp = byte.MaxValue;
                    if( run != null && run.Length > 0 &&
                        byte.TryParse( run, out tmp ) &&
                        tmp < program.Length &&
                        File.Exists( program[tmp] + ".exe" ) )
                    {
                        this.buttonCancel.Enabled = true;
                        this.buttonCancel.Text = "Run " + program[tmp];
                        if( updater.FOnlineCfg.GetValue( "FOConfig", "SkipLogin", false ) )
                            this.buttonCancel.Tag = new string[] { program[tmp], "-Start" };
                        else
                            this.buttonCancel.Tag = new string[] { program[tmp], null };
                    }
                    else
                    {
                        this.buttonCancel.SetEnabled( false );
                    }
                }
            }
            else if( result == FOUpdaterResult.InvalidConfig )
            {
                this.buttonCancel.SetEnabled( false );
                this.buttonCancel.SetVisible( false );
            }
            else if( result == FOUpdaterResult.NoSource )
            {
                this.buttonCancel.SetEnabled( false );
                this.buttonCancel.SetVisible( false );
            }
            else
                throw new NotImplementedException( Enum.GetName( typeof( FOUpdaterResult ), result ) );

            this.buttonExit.SetEnabled( true );

        }

        private void buttonCancel_Click( object sender, EventArgs e )
        {
            Button self = (Button)sender;

            if( self.Tag != null )
            {
                string[] run = (string[])self.Tag;
                if( run.Length == 2 )
                {
                    if( run[1] == null )
                        Process.Start( run[0] );
                    else
                        Process.Start( run[0], run[1] );

                    Environment.Exit( 0 );
                }
            }
            else
                updater.Cancelled = true;
        }

        private void buttonExit_Click( object sender, EventArgs e )
        {
            this.Close();
        }


        void frmUpdater_Load( object sender, EventArgs e )
        {
            this.backgroundWorker.RunWorkerAsync();
        }

        #endregion
    }

    static class Extensions
    {
        private delegate void delegate_bool( bool _bool_ );
        private delegate void delegate_int( int _int_ );
        private delegate void delegate_string( string _string_ );
        private delegate void delegate_object( object _object_ );

        public static void SetText( this Label label, string value )
        {
            if( label.InvokeRequired )
                label.Invoke( new delegate_string( label.SetText ), value );
            else
                label.Text = value;
        }

        public static void SetValue( this ProgressBar progress, int value )
        {
            string dbg = progress.Name;

            if( progress.InvokeRequired )
                progress.Invoke( new delegate_int( progress.SetValue ), value );
            else
                progress.Value = value;
        }

        public static void SetMaximum( this ProgressBar progress, int value )
        {
            if( progress.InvokeRequired )
                progress.Invoke( new delegate_int( progress.SetMaximum ), value );
            else
                progress.Maximum = value;
        }

        public static void SetVisible( this Button button, bool value )
        {
            if( button.InvokeRequired )
                button.Invoke( new delegate_bool( button.SetVisible ), value );
            else
                button.Visible = value;
        }

        public static void SetEnabled( this Button button, bool value )
        {
            if( button.InvokeRequired )
                button.Invoke( new delegate_bool( button.SetEnabled ), value );
            else
                button.Enabled = value;
        }

        public static void SetText( this Button button, string value )
        {
            if( button.InvokeRequired )
                button.Invoke( new delegate_string( button.SetText ), value );
            else
                button.Text = value;
        }

        public static void SetTag( this Button button, object value )
        {
            if( button.InvokeRequired )
                button.Invoke( new delegate_object( button.SetTag ), value );
            else
                button.Tag = value;
        }
    }
}
