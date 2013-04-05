using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

// for extensions in .net 2.0
namespace System.Runtime.CompilerServices
{
    [AttributeUsage( AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method )]
    public sealed class ExtensionAttribute : Attribute { }
}

namespace FOUpdater
{
    static class FOUpdater
    {
        public static frmMain WindowUpdater;
        public static bool AutoClose = false;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main( string[] args )
        {
            string cmdSource = null;
            string cmdConfig = null;

            foreach( string arg in args )
            {
                if( arg == "/autoclose" || arg == "--autoclose" )
                    AutoClose = true;

                else if( arg.StartsWith( "/source:" ) || arg.StartsWith( "--source:" ) )
                    cmdSource = arg.Substring( (arg.StartsWith( "/source:" ) ? 8 : 9) );

                else if( arg.StartsWith( "/config:" ) || arg.StartsWith( "--config:" ) )
                    cmdConfig = arg.Substring( (arg.StartsWith( "/config:" ) ? 8 : 9) );
            }

            FOUpdaterClient updater = new FOUpdaterClient( cmdConfig );

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault( false );

            WindowUpdater = new frmMain( cmdSource );
            WindowUpdater.Text += " v" + FOUpdaterClient.VersionString;

            if( updater.FOnlineCfg == null && cmdSource == null )
            {
                MessageBox.Show(
                    "Cannot find FOnline configuration file in current directory:\n" +
                    Path.GetDirectoryName( Application.ExecutablePath ) + "\n",
                    "FOUpdater",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                Environment.Exit( 0 );
            }

            Application.Run( WindowUpdater );

            /*
            if( Installer && File.Exists( ".\\FOConfig.exe" ) )
            {
                DialogResult result = MessageBox.Show(
                    "To finish installation process, you have to\n" +
                    "pleple\n" +
                    "\n" +
                    "Do you wish to run configuration tool?",
                    "Updater",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information
                );

                if( result == DialogResult.No )
                    Environment.Exit( 0 ); 
                else
                    Process.Start( ".\\FOConfig.exe" );
            }
            */
        }

        public static void Message( string msg )
        {
            MessageBox.Show( msg, "Message", MessageBoxButtons.OK, MessageBoxIcon.Information );
        }
    }
}
