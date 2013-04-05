using System;

using FOUpdater;

namespace FOUpdater
{
    static class FOUpdater
    {
        private const int textGeneral = 0;
        private const int textDetailed = 1;
        private const bool showAnyKey = true;
        private const int textAnyKey = 3;
        private static bool AutoClose = false;

        private static int[] messageLength = new int[Enum.GetValues( typeof( FOUpdaterMessage ) ).Length];

        static void Main( string[] args )
        {
            Console.Title = "FOUpdater v" + FOUpdaterClient.VersionString;
            Console.CursorVisible = false;
            Console.Clear();

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

            FOUpdaterResult result = updater.Update( consoleMessage, null, cmdSource );

            if( result == FOUpdaterResult.InvalidConfig )
            {
                consoleMessage( FOUpdaterMessage.General, "Invalid configuration" );
            }

            if( !AutoClose )
            {
                Console.SetCursorPosition( 0, textAnyKey );
                Console.CursorVisible = true;
                if( showAnyKey )
                {
                    Console.Write( "Press any key to exit..." );
                    Console.ReadKey( true ); ;
                }
            }
        }

        private static void consoleMessage( FOUpdaterMessage type, string message )
        {
            int top = -1;
            switch( type )
            {
                case FOUpdaterMessage.General:
                    top = textGeneral;
                    break;
                case FOUpdaterMessage.Detailed:
                    top = textDetailed;
                    break;
                case FOUpdaterMessage.Info:
                    top = textAnyKey + 2;
                    break;
                default:
                    throw new NotImplementedException( Enum.GetName( typeof( FOUpdaterMessage ), type ) );
            }
            Console.SetCursorPosition( 0, top );
            if( message != null )
            {
                Console.Write( message );
                for( int i = message.Length; i < messageLength[(int)type]; i++ )
                {
                    Console.Write( " " );
                }
                messageLength[(int)type] = message.Length;
            }
            else
            {
                for( int i = 0; i < messageLength[(int)type]; i++ )
                {
                    Console.Write( " " );
                }
                messageLength[(int)type] = 0;
            }
        }
    }
}
