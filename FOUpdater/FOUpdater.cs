using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;

using Ionic.Zlib;

namespace FOUpdater
{
    public enum FOUpdaterResult
    {
        /// <summary>
        /// Update completed
        /// </summary>
        OK = 0,

        /// <summary>
        /// Update process has been canceled
        /// </summary>
        Cancel,

        /// <summary>
        /// Configur file not found, and no source given
        /// </summary>
        NoConfig,

        /// <summary>
        /// No sources found in config file
        /// </summary>
        InvalidConfig,

        /// <summary>
        /// Cannot connect to any of given sources
        /// </summary>
        NoSource
    }

    public enum FOUpdaterMessage
    {
        General = 0,
        Detailed,
        Info
    }

    public enum FOUpdaterProgress
    {
        FileValue = 0,
        FileMaximum,
        TotalValue,
        TotalMaximum,
    }

    public enum FOUpdateType
    {
        Unknown = 0,
        Stream,
        HTTP,
        FTP, // TODO
    }

    public delegate void UpdaterMessage( FOUpdaterMessage type, string text );
    public delegate void UpdaterProgress( FOUpdaterProgress type, uint value );

    delegate void UpdaterFileProgress( UpdateFile updateFile, long current, long total, uint percent );
    delegate void UpdaterFileSave( UpdateFile updateFile );
    delegate void UpdaterFileComplete( UpdateFile updateFile );

    public class FOUpdaterClient
    {
        public static Version Version = new Version( 0, 2, 0, 5 );
        public static string VersionString
        {
            get
            {
                string result = Version.Major + "." + Version.Minor;
                if( Version.Build + Version.Revision > 0 )
                {
                    result += "." + Version.Build;
                    if( Version.Revision > 0 )
                        result += "." + Version.Revision;
                }
                return (result);
            }
        }

        /// <summary>
        /// Holds all strings used in update client
        /// </summary>
        public static class Strings
        {
            /// <summary>
            /// Configuration filename, storing all FOUpdater settings
            /// </summary>
            public const string ConfigFile = "FOnline.cfg";

            ///////////////////
            /// Config file ///
            ///////////////////

            /// <summary>
            /// Section in ConfigFile where all settings are stored;
            /// must be different than FOnline client settings section ("Game Options")
            /// </summary>
            public const string Section = "Updater";

            /// <summary>
            /// Makes FOUpdater checking sources in randomized order
            /// </summary>
            public const string RandomSource = "Random" + Source;

            public const string Run = "Run";

            /// <summary>
            /// 
            /// </summary>
            public const string Source = "Source";

            /////////////////////////////////////////////////
            /// Various messages sent to main application ///
            /////////////////////////////////////////////////

            public const string Initializing = "Initializing...";
            public const string NoSources = "No update server(s) available";
            public const string Cancelled = "Update cancelled";
            public const string CheckingHost = "Checking %s";
            public const string CheckingFiles = "Checking files...";
            public const string UpdateNotNeeded = "Update not needed";
            public const string Downloading = "Downloading %s";
            public const string DownloadingInfo = "%c / %t (%p%)";
            public const string Saving = "Saving %s";
            public const string UpdatedFiles = "Updated %u file(s)";
            public const string DeletedFiles = "Deleted %d file(s)";
            public const string UpdatedDeletedFiles = "Updated %u file(s), deleted %d files";
        }

        private bool cancelled = false;
        /// <summary>
        /// Holds information if update process has been cancelled
        /// <para>Once set to true, cannot be changed back</para>
        /// </summary>
        public bool Cancelled
        {
            get { return (this.cancelled); }
            set
            {
                if( this.cancelled == true )
                    return;
                else
                    this.cancelled = true;
            }
        }

        public IniReader FOnlineCfg { get; private set; }

        public List<string> Sources { get; private set; }

        public bool RandomSource { get; private set; }

        /// <summary>
        /// Callback used for sending text to main application
        /// </summary>
        private UpdaterMessage message = null;

        /// <summary>
        /// Callback used for sending download progress to main application
        /// </summary>
        private UpdaterProgress progress = null;

        private readonly object locker = new object();
        private uint progressTotal = 0;

        /// <summary>
        /// Create FOUpdater Client
        /// </summary>
        public FOUpdaterClient( string fonlineCfg = null )
        {
            // be annoying
            if( Strings.Section.ToLower() == "game options" )
            {
                System.Diagnostics.Debug.Assert( false, "Invalid FOUpdaterClient::Strings::Section", "FOUpdaterClient::Strings::Section cannot be \"" + Strings.Section + "\"" );
                Environment.Exit( -1 );
            }

            this.Sources = new List<string>();
            this.RandomSource = false;

            Directory.SetCurrentDirectory( Directory.GetParent( Assembly.GetExecutingAssembly().Location ).ToString() );

            if( fonlineCfg != null && File.Exists( fonlineCfg ) )
                this.FOnlineCfg = new IniReader( fonlineCfg );
            else
            {
                if( File.Exists( ".\\" + Strings.ConfigFile ) )
                    this.FOnlineCfg = new IniReader( ".\\" + Strings.ConfigFile );
            }
        }

        /// <summary>
        /// Core of FOUpdater,
        /// 
        /// </summary>
        /// <param name="_message">callback for writing messages</param>
        /// <param name="_progress">callback for </param>
        /// <returns></returns>
        public FOUpdaterResult Update( UpdaterMessage _message, UpdaterProgress _progress, string defaultSource = null )
        {
            lock( locker )
            {
                this.message = _message;
                this.progress = _progress;

                this.SendMessage( FOUpdaterMessage.General, Strings.Initializing );
            }

            List<string> sources = new List<string>();

            if( defaultSource != null )
            {
                if( this.GetUpdateType( defaultSource ) != FOUpdateType.Unknown )
                    sources.Add( defaultSource );

                if( this.FOnlineCfg == null && !File.Exists( "FOnline.cfg" ) )
                {
                    using( StreamWriter file = File.CreateText( ".\\" + Strings.ConfigFile ) )
                    {
                        file.Flush();
                        file.Close();
                    }
                    if( File.Exists( ".\\" + Strings.ConfigFile ) )
                    {
                        this.FOnlineCfg = new IniReader( ".\\" + Strings.ConfigFile );
                        this.FOnlineCfg.IniWriteValue( Strings.Section, Strings.Source + "0", defaultSource );
                        this.Sources.Add( defaultSource );
                    }
                }
            }
            else // defaultSource == null
            {
                if( this.FOnlineCfg != null )
                {
                    for( uint s = 0; s < uint.MaxValue; s++ )
                    {
                        string source = this.FOnlineCfg.IniReadValue( Strings.Section, Strings.Source + s );
                        if( source == null || source.Length == 0 )
                            break;
                        else if( this.GetUpdateType( source ) != FOUpdateType.Unknown )
                            sources.Add( source );
                    }
                    if( sources.Count == 0 )
                        return (FOUpdaterResult.InvalidConfig);
                }
                else
                    return (FOUpdaterResult.NoConfig);
            }

            if( sources.Count == 0 )
                return (FOUpdaterResult.InvalidConfig);

            if( this.FOnlineCfg.IniReadValueBool( FOUpdaterClient.Strings.Section, FOUpdaterClient.Strings.RandomSource ) )
            {
                List<string> randomList = new List<string>();
                Random random = new Random( (int)DateTime.Now.Ticks );
                int count = sources.Count;
                while( randomList.Count != count )
                {
                    string element = sources[random.Next( 0, sources.Count )];

                    if( randomList.Contains( element ) )
                        continue;

                    randomList.Add( element );
                    sources.Remove( element );
                }

                sources = randomList;
            }

            this.Sources = sources;

            uint updated = 0, deleted = 0;
            bool foundSource = false;
            foreach( string source in this.Sources )
            {
                bool nextUpdateType = false;

                FOUpdateType type = this.GetUpdateType( source );
                UpdateInfo info = null;
                if( type == FOUpdateType.Stream )
                    info = new UpdateInfoStream( this );
                else if( type == FOUpdateType.HTTP )
                    info = new UpdateInfoHTTP( this );
                /*
                else if( type == UpdateType.FTP )
                    info = new UpdateInfoFTP( this );
                */
                else
                    continue;

                if( info.Start( defaultSource ) )
                {
                    foundSource = true;

                    lock( this.locker )
                    {
                        this.SendMessage( FOUpdaterMessage.General, Strings.CheckingFiles );
                    }

                    List<UpdateFile> needUpdate = new List<UpdateFile>();

                    this.SendProgress( FOUpdaterProgress.FileValue, 0 );
                    this.SendProgress( FOUpdaterProgress.FileMaximum, 0 );

                    this.SendProgress( FOUpdaterProgress.TotalValue, 0 );
                    this.SendProgress( FOUpdaterProgress.TotalMaximum, 0 );

                    uint progressMax = 0;
                    foreach( UpdateFile file in info.Files )
                    {
                        lock( this.locker )
                        {
                            if( file.Delete )
                            {
                                if( File.Exists( file.FileName ) )
                                {
                                    File.Delete( file.FileName );
                                    deleted++;
                                }
                                continue;
                            }
                            if( file.NeedUpdate )
                            {
                                progressMax += file.Size;
                                this.SendProgress( FOUpdaterProgress.TotalMaximum, progressMax );
                                needUpdate.Add( file );
                            }
                        }
                    }

                    if( needUpdate.Count == 0 )
                    {
                        lock( this.locker )
                        {
                            this.SendMessage( FOUpdaterMessage.General,
                                (deleted > 0
                                    ? Strings.DeletedFiles
                                    : Strings.UpdateNotNeeded
                            ) );

                        }

                        info.End();
                        return (FOUpdaterResult.OK);
                    }

                    foreach( UpdateFile file in needUpdate )
                    {
                        lock( this.locker )
                        {
                            if( this.Cancelled )
                                break;

                            this.SendProgress( FOUpdaterProgress.FileValue, 0 );
                            this.SendProgress( FOUpdaterProgress.FileMaximum, file.Size );

                            this.SendMessage( FOUpdaterMessage.General, Strings.Downloading.Replace( "%s", file.FileName ) );
                        }

                        file.PrepareDirectory();

                        if( !file.Download( fileProgress, fileSave, fileComplete ) )
                        {
                            if( this.Cancelled )
                                break;
                            else
                                nextUpdateType = true;
                        }
                        else
                            updated++;


                        lock( locker )
                        {
                            this.progressTotal += file.Size;
                            this.SendProgress( FOUpdaterProgress.TotalValue, this.progressTotal );
                        }
                    }

                    info.End();
                }
                else
                    nextUpdateType = true;

                if( this.Cancelled || !nextUpdateType )
                    break;
            }

            FOUpdaterResult result = FOUpdaterResult.OK;

            lock( this.locker )
            {
                this.SendMessage( FOUpdaterMessage.Detailed, null );

                if( !foundSource )
                {
                    this.SendMessage( FOUpdaterMessage.General, Strings.NoSources );
                    result = FOUpdaterResult.NoSource;
                }
                else if( this.Cancelled )
                {
                    this.SendMessage( FOUpdaterMessage.General, Strings.Cancelled );
                    result = FOUpdaterResult.Cancel;
                }
                else
                {
                    this.SendProgress( FOUpdaterProgress.FileValue, 0 );
                    this.SendProgress( FOUpdaterProgress.FileMaximum, 0 );

                    this.SendMessage( FOUpdaterMessage.General, (deleted > 0
                        ? Strings.UpdatedDeletedFiles.Replace( "%u", "" + updated ).Replace( "%d", "" + deleted )
                        : Strings.UpdatedFiles.Replace( "%u", "" + updated )) );
                }
            }

            return (result);
        }

        /// <summary>
        /// Sends status text to main application
        /// </summary>
        public void SendMessage( FOUpdaterMessage type, string text )
        {
            lock( this.locker )
            {
                if( this.message != null )
                    this.message( type, text );
            }
        }

        /// <summary>
        /// Sends progress update to main application
        /// </summary>
        public void SendProgress( FOUpdaterProgress type, uint value )
        {
            lock( this.locker )
            {
                if( this.progress != null )
                    this.progress( type, value );
            }
        }

        private void fileProgress( UpdateFile updateFile, long currentBytes, long totalBytes, uint percent )
        {
            lock( this.locker )
            {
                this.SendMessage( FOUpdaterMessage.Detailed, Strings.DownloadingInfo
                    .Replace( "%c", ToHumanString( currentBytes, true ) )
                    .Replace( "%t", ToHumanString( totalBytes, true ) )
                    .Replace( "%p", "" + percent )
                );
                this.SendProgress( FOUpdaterProgress.FileValue, (uint)currentBytes );
                this.SendProgress( FOUpdaterProgress.TotalValue, progressTotal + (uint)currentBytes );
            }
        }

        private void fileSave( UpdateFile updateFile )
        {
            lock( this.locker )
            {
                this.SendMessage( FOUpdaterMessage.General, Strings.Saving.Replace( "%s", updateFile.FileName ) );
            }
        }

        private void fileComplete( UpdateFile updateFile )
        {
            lock( this.locker )
            {
                this.SendMessage( FOUpdaterMessage.Detailed, null );
                this.SendProgress( FOUpdaterProgress.FileValue, 0 );
            }
        }

        public FOUpdateType GetUpdateType( string source )
        {
            if( source == null || source.Length < 5 || !source.Contains( "://" ) )
                return (FOUpdateType.Unknown);

            foreach( string type in Enum.GetNames( typeof( FOUpdateType ) ) )
            {
                if( type.ToLower() == Enum.GetName( typeof( FOUpdateType ), FOUpdateType.Unknown ).ToLower() )
                    continue;
                if( source.StartsWith( type.ToLower() + "://" ) )
                    return ((FOUpdateType)Enum.Parse( typeof( FOUpdateType ), type ));
            }

            return (FOUpdateType.Unknown);
        }

        private readonly static long Kilobyte = 1024;
        private readonly static long Megabyte = 1024 * Kilobyte;
        private readonly static long Gigabyte = 1024 * Megabyte;
        private readonly static long Terabyte = 1024 * Gigabyte;
        // TODO: add more? :P

        private static string ToHuman( ref long size, long b, string what )
        {
            if( size > 0 && size >= b )
            {
                long xb = size / b;
                size -= xb * b;
                return (xb + what);
            }
            return ("");
        }

        private static string ToHumanString( long size, bool getFirst = false )
        {
            string result = "";
            char[] trimEnd = new char[] { ',', ' ' };

            result += ToHuman( ref size, Terabyte, "TB, " );
            if( getFirst && result.Length > 0 )
                return (result.TrimEnd( trimEnd ));

            result += ToHuman( ref size, Gigabyte, "GB, " );
            if( getFirst && result.Length > 0 )
                return (result.TrimEnd( trimEnd ));

            result += ToHuman( ref size, Megabyte, "MB, " );
            if( getFirst && result.Length > 0 )
                return (result.TrimEnd( trimEnd ));

            result += ToHuman( ref size, Kilobyte, "KB, " );
            if( getFirst && result.Length > 0 )
                return (result.TrimEnd( trimEnd ));

            if( size > 0 ) result += size + "B";

            return (result.TrimEnd( trimEnd ));
        }
    }

    public class FOUpdaterServer
    {
        // TODO
    }

    #region Base classes

    /// <summary>
    /// Represents single update info source
    /// </summary>
    class UpdateInfo
    {
        public FOUpdateType Type { get; private set; }
        public FOUpdaterClient Parent { get; private set; }

        public string Name
        {
            get { return (Enum.GetName( typeof( FOUpdateType ), this.Type )); }
        }

        public string Prefix
        {
            get { return (this.Name.ToLower() + "://"); }
        }

        public uint MinimumInfoVersion { get; protected set; }
        public uint MaximumInfoVersion { get; protected set; }

        private uint version = 0;
        public uint InfoVersion
        {
            get { return (this.version); }
            protected set
            {
                if( value < this.MinimumInfoVersion )
                    this.version = this.MinimumInfoVersion;
                else if( value > this.MaximumInfoVersion )
                    this.version = this.MaximumInfoVersion;
                else
                    this.version = value;
            }
        }

        /// <summary>
        /// If info about files is in text form,
        /// it would be kept here in raw format
        /// </summary>
        public string Content { get; protected set; }

        /// <summary>
        /// List of files to check
        /// </summary>
        public List<UpdateFile> Files { get; private set; }

        public uint FilesTotalSize
        {
            get
            {
                uint result = 0;
                foreach( UpdateFile file in this.Files )
                {
                    result += file.Size;
                }
                return (result);
            }
        }

        public UpdateFile this[string fileName]
        {
            get
            {
                foreach( UpdateFile file in this.Files )
                {
                    if( file.FileName == fileName )
                        return (file);
                }
                return (null);
            }
        }

        public UpdateInfo( FOUpdaterClient parent, FOUpdateType type )
        {
            if( parent == null )
                throw new ArgumentNullException( "parent" );

            if( type == FOUpdateType.Unknown )
                throw new ArgumentException( "type" );

            this.Parent = parent;
            this.Type = type;
            this.MinimumInfoVersion = this.MaximumInfoVersion = this.InfoVersion = 0;
            this.Content = "";
            this.Files = new List<UpdateFile>();
        }

        protected List<string> GetSourcesList()
        {
            List<string> result = new List<string>();

            foreach( string source in this.Parent.Sources )
            {
                if( source == null || source.Length == 0 ||
                    this.Parent.GetUpdateType( source ) != this.Type )
                    continue;

                result.Add( source );
            }

            return (result);
        }

        /// <summary>
        /// Initializes UpdateInfo.
        /// </summary>
        /// <param name="defaultSettings">ignored in base class</param>
        /// <returns>always true</returns>
        public virtual bool Start( string defaultSettings = null )
        {
            return (true);
        }

        /// <summary>
        /// Finalize UpdateInfo.
        /// </summary>
        public virtual void End()
        {
        }
    }

    /// <summary>
    /// Represents single file to check
    /// </summary>
    class UpdateFile
    {
        #region UpdateFile/static

        private static uint FileID = 0;

        #endregion

        public UpdateInfo Parent { get; protected set; }

        public readonly uint CRC = 0;
        public readonly uint Size = 0;
        public readonly string FileName = null;
        public readonly List<string> Options = new List<string>();

        /// <summary>
        /// True if file is supposed to be updated with every check
        /// </summary>
        public bool Always
        {
            get { return (this.Options.Contains( "always" )); }
            protected set
            {
                if( !this.Always )
                    this.Options.Add( "always" );
            }
        }

        /// <summary>
        /// True if file is supposed to be deleted from client directory
        /// </summary>
        public bool Delete
        {
            get { return (this.Options.Contains( "delete" )); }
            protected set
            {
                if( !this.Delete )
                    this.Options.Add( "delete" );
            }
        }

        /// <summary>
        /// True if file is supposed to be downloaded only once
        /// </summary>
        public bool NoRewrite
        {
            get { return (this.Options.Contains( "norewrite" )); }
            protected set
            {
                if( !this.NoRewrite )
                    this.Options.Add( "norewrite" );
            }
        }

        /// <summary>
        /// True if file is sent in packed form. Subclass must perform unpacking
        /// on its own
        /// </summary>
        public bool Packed
        {
            get { return (this.Options.Contains( "pack" )); }
            protected set
            {
                if( !this.Always )
                    this.Options.Add( "pack" );
            }
        }

        /// <summary>
        /// True if file needs to be updated. Checking order/results:
        /// <para>- if file doesn't exists and option Delete is not set: true</para>
        /// <para>- if option NoRewrite or Delete is set: false</para>
        /// <para>- if option Always is set: true</para>
        /// <para>- if CRC doesn't match: true</para>
        /// <para>- false</para>
        /// </summary>
        public bool NeedUpdate
        {
            get
            {
                if( !File.Exists( this.FileName ) && !this.Delete )
                    return (true);

                if( this.NoRewrite || this.Delete )
                    return (false);

                if( this.Always )
                    return (true);

                if( CRC32.Get( this.FileName ) != this.CRC )
                    return (true);

                return (false);
            }
        }

        // internal

        public readonly uint ID = 0;

        /// <summary>
        /// Returns Parent' UpdateType
        /// </summary>
        public FOUpdateType Type
        {
            get { return (this.Parent.Type); }
        }

        /// <summary>
        /// Create UpdateFile instance
        /// </summary>
        /// <param name="parent">
        /// <para>throws ArgumentNullException if null</para>
        /// <para>throws ArgumentException if parent type is UpdateType::Unknown</para>
        /// </param>
        /// <param name="crc">expected CRC32 of file</param>
        /// <param name="size">expected size of file, can be different than final size if file is packed</param>
        /// <param name="filename">final name of file, including relative directory</param>
        /// <param name="options"></param>
        /// 
        /// <exception cref="ArgumentException">A</exception>
        protected UpdateFile( UpdateInfo parent, uint crc, uint size, string filename, List<string> options )
        {
            if( parent == null )
                throw new ArgumentNullException( "parent" );
            else if( parent.Type == FOUpdateType.Unknown ) // should never happen
                throw new ArgumentException( "parent" );

            this.CRC = crc;
            this.Size = size;
            this.FileName = filename.Replace( '\\', '/' );

            this.Options = options;
            this.ID = ++FileID;

            // TODO?: this is probably not the best way, but let it stay for now
            this.Parent = parent;
            this.Parent.Files.Add( this );
        }

        /// <summary>
        /// Create directory tree for file, if needed
        /// </summary>
        public void PrepareDirectory()
        {
            string dir = Path.GetDirectoryName( this.FileName );
            if( dir.Length > 0 && !Directory.Exists( dir ) )
                Directory.CreateDirectory( dir );
        }

        /// <summary>
        /// Downloads file from update source.
        /// <para>NOTE: Must be overriden by subclass.</para>
        /// </summary>
        /// <param name="progress">function called when progress percentage changes</param>
        /// <param name="save">function called before file is saved</param>
        /// <param name="complete">function called when download is finished</param>
        public virtual bool Download( UpdaterFileProgress progress, UpdaterFileSave save, UpdaterFileComplete complete )
        {
            throw new NotImplementedException( "FOUpdateFile(" + Enum.GetName( typeof( FOUpdateType ), this.Type ) + ")::Download" );
        }
    }

    #endregion // Base classes

    #region SubClasses (UpdateInfo*, UpdateFile*)

    /// <summary>
    /// Update files are taken from UpdaterServer, included in FOnline SDK/TLA
    /// </summary>
    sealed class UpdateInfoStream : UpdateInfo
    {
        public string Host { get; private set; }
        public uint Port { get; private set; }

        public TcpClient Client { get; private set; }

        public UpdateInfoStream( FOUpdaterClient parent )
            : base( parent, FOUpdateType.Stream )
        {
            this.MinimumInfoVersion = 1;
            this.MaximumInfoVersion = 2;
        }

        public override bool Start( string defaultSetting = null )
        {
            List<string> streamList = new List<string>();

            if( defaultSetting != null && defaultSetting.Length > 0 )
            {
                if( defaultSetting.StartsWith( this.Prefix ) &&
                    this.Parent.GetUpdateType( defaultSetting ) == this.Type )
                    streamList.Add( defaultSetting );
                else
                    return (false);
            }
            else
            {
                List<string> streamTmp = this.GetSourcesList();
                if( streamTmp == null || streamTmp.Count == 0 )
                    return (false);
                else
                    streamList = streamTmp;
            }

            if( streamList.Count == 0 )
                return (false);

            foreach( string streamLine in streamList )
            {
                if( !streamLine.StartsWith( this.Prefix ) )
                    continue;

                string[] stream = streamLine.Remove( 0, this.Prefix.Length ).Split( new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries );
                uint port = 4040, version = 1;
                if( stream.Length >= 2 )
                    uint.TryParse( stream[1], out port );

                if( stream.Length >= 3 )
                    uint.TryParse( stream[2], out version );

                if( version < this.MinimumInfoVersion || version > this.MaximumInfoVersion )
                    return (false);

                try
                {
                    this.Parent.SendMessage( FOUpdaterMessage.General, FOUpdaterClient.Strings.CheckingHost.Replace( "%s", stream[0] + ":" + port ) );
                    this.Client = new TcpClient( stream[0], (int)port );
                }
                catch( SocketException )
                {
                    this.Client = null;
                    continue;
                }

                this.Host = stream[0];
                this.Port = port;
                this.InfoVersion = version;

                if( !this.SendCommand( "Hello" + version, 1000 ) )
                    return (false);

                if( !this.SendCommand( "Give hashes list", -1 ) )
                    return (false);

                break;

            }

            if( this.Client == null )
                return (false);

            return (true);
        }

        public override void End()
        {
            base.End();

            this.SendCommand( "Bye", 0 );
        }

        private bool IsConnected()
        {
            if( this.Client == null ||
                !this.Client.Connected ||
                !this.Client.GetStream().CanRead ||
                !this.Client.GetStream().CanWrite )
                return (false);

            return (true);
        }

        public byte[] Read( uint size )
        {
            byte[] data = new byte[size];

            if( this.IsConnected() )
                this.Client.GetStream().Read( data, 0, (int)size );

            return (data);
        }

        /// <summary>
        /// Read single uint from stream
        /// </summary>
        /// <returns></returns>
        public uint ReadUint()
        {
            do { Thread.Sleep( 50 ); } while( this.Client.Available < 4 );

            byte[] data = this.Read( 4 );

            return (BitConverter.ToUInt32( data, 0 ));
        }

        public string ReadLine()
        {
            StringBuilder result = new StringBuilder();
            bool wait = true;
            while( wait == true )
            {
                int input = this.Client.GetStream().ReadByte();
                if( input == -1 )
                    continue;

                result.Append( (char)input );

                if( result.Length >= 2 &&
                    result[result.Length - 2] == '\r' &&
                    result[result.Length - 1] == '\n' )
                    wait = false;
            }

            return (result.Remove( result.Length - 2, 2 ).ToString());
        }

        public bool Send( string text )
        {
            if( !this.IsConnected() )
                return (false);

            byte[] data = ASCIIEncoding.ASCII.GetBytes( text );
            this.Client.GetStream().Write( data, 0, data.Length );

            return (true);
        }

        public bool SendCommand( string text, int answerTime, bool addNewLine = true )
        {
            if( !this.Send( text + (addNewLine ? "\n" : "") ) )
                return (false);

            if( answerTime < 0 )
                do { } while( !Client.GetStream().DataAvailable );
            else
                Thread.Sleep( answerTime );

            if( text == "Bye" )
                return (true);

            string result = this.ReadLine();

            if( result == "Greetings" )
            {
                if( InfoVersion == 2 )
                {
                    this.ReadLine(); // GameFileName
                    this.ReadLine(); // UpdaterURL
                    this.ReadLine(); // GameServer
                    this.ReadLine(); // GameServerPort
                    this.ReadLine(); // reserved
                    this.ReadLine(); // reserved
                    this.ReadLine(); // reserved
                }
            }
            else if( result.StartsWith( "Take hashes list" ) )
            {
                bool extended = result.StartsWith( "Take hashes list extended" );
                uint hashesCount = this.ReadUint();
                this.Content = "";

                for( uint i = 0; i < hashesCount; i++ )
                {
                    string filename = this.ReadLine();
                    this.Content += filename + "\n";

                    uint crc = 0;
                    uint size = 0;
                    string optionsRaw = "";

                    if( extended )
                    {
                        int crc_ = 0;
                        int.TryParse( this.ReadLine(), out crc_ );
                        crc = (uint)crc_;

                        uint.TryParse( this.ReadLine(), out size );

                        this.Content += crc_ + "\n" + size + "\n";

                        optionsRaw = this.ReadLine();
                        if( optionsRaw.Length > 0 )
                            this.Content += optionsRaw + "\n";
                    }
                    else
                    {
                        uint.TryParse( this.ReadLine(), out crc );

                        this.Content += crc + "\n";
                    }

                    List<string> options = new List<string>( optionsRaw.TrimStart( '<' ).TrimEnd( '>' ).Split( new string[] { "><" }, StringSplitOptions.RemoveEmptyEntries ) );

                    new UpdateFileStream( this, crc, size, filename, options );
                }
            }

            return (true);
        }
    }

    /// <summary>
    /// Update files are taken from http server, see also Resources/FOUpdater.php
    /// </summary>
    sealed class UpdateInfoHTTP : UpdateInfo
    {
        /// <summary>
        /// Address of used info file
        /// </summary>
        public string Address { get; private set; }

        /// <summary>
        /// Address used to build direct URL to update file
        /// </summary>
        public string RootAddress
        {
            get { return (this.Address.Substring( 0, this.Address.LastIndexOf( '/' ) + 1 )); }
        }

        public UpdateInfoHTTP( FOUpdaterClient parent )
            : base( parent, FOUpdateType.HTTP )
        {
            this.MinimumInfoVersion = this.MaximumInfoVersion = 1;
        }

        public override bool Start( string defaultSetting = null )
        {
            if( !base.Start( null ) )
                return (false);

            List<string> httpList = new List<string>();

            if( defaultSetting != null && defaultSetting.Length > 0 )
            {
                if( defaultSetting.StartsWith( this.Prefix ) &&
                    this.Parent.GetUpdateType( defaultSetting ) == this.Type )
                    httpList.Add( defaultSetting );
                else
                    return (false);
            }
            else
            {
                List<string> httpTmp = this.GetSourcesList();
                if( httpTmp == null || httpTmp.Count == 0 )
                    return (false);
                else
                    httpList = httpTmp;
            }

            if( httpList.Count == 0 )
                return (false);

            foreach( string url in httpList )
            {
                string content = this.Content = null;
                try
                {
                    this.Parent.SendMessage( FOUpdaterMessage.General, FOUpdaterClient.Strings.CheckingHost.Replace( "%s", url ) );
                    WebClient client = new WebClient();
                    client.Headers[HttpRequestHeader.UserAgent] = "FOUpdater/" + FOUpdaterClient.VersionString;
                    content = client.DownloadString( url );
                }
                catch
                {
                    continue;
                }

                if( content != null )
                {
                    this.InfoVersion = 1;
                    this.Address = url;
                    this.Content = content;

                    break;
                }
            }

            if( Content == null )
                return (false);

            foreach( string line in Content.Split( '\n' ) )
            {
                string[] vars = line.Trim( new char[] { ' ', '\n', '\r', '\t' } ).Split( ' ' );

                if( vars.Length >= 3 )
                {
                    if( vars[0] == "config" )
                    {
                        if( this.Parent.FOnlineCfg != null )
                            this.Parent.FOnlineCfg.IniWriteValue( FOUpdaterClient.Strings.Section, vars[1], vars[2] );
                        continue;
                    }
                    else if( vars[0] == "updater" )
                    {
                        // TODO
                        continue;
                    }

                    uint crc = 0;
                    uint size = 0;

                    if( !uint.TryParse( vars[0], out crc ) ||
                        !uint.TryParse( vars[1], out size ) ||
                        vars[2].Length == 0 )
                        continue;

                    List<string> options = new List<string>();
                    if( vars.Length >= 4 )
                    {
                        options = new List<string>( vars[3].ToLower().Split( ',' ) );
                    }

                    new UpdateFileHTTP( this, crc, size, vars[2], options );
                }
            }

            return (true);
        }

        public override void End()
        {
            base.End();
        }
    }

    /// <summary>
    /// TODO
    /// </summary>
    sealed class UpdateInfoFTP : UpdateInfo
    {
        public UpdateInfoFTP( FOUpdaterClient parent )
            : base( parent, FOUpdateType.FTP )
        {
            throw new NotImplementedException();
        }

        public override bool Start( string defaultSetting = null )
        {
            List<string> ftpList = new List<string>();

            if( defaultSetting != null && defaultSetting.Length > 0 )
            {
                if( defaultSetting.StartsWith( this.Prefix ) &&
                    this.Parent.GetUpdateType( defaultSetting ) == this.Type )
                    ftpList.Add( defaultSetting );
                else
                    return (false);
            }
            else
            {
                List<string> ftpTmp = this.GetSourcesList();

                if( ftpTmp == null || ftpTmp.Count == 0 )
                    return (false);
                else
                    ftpList = ftpTmp;
            }

            if( ftpList.Count == 0 )
                return (false);

            foreach( string url in ftpList )
            {
                return (false);
            }

            return (false);
        }

        public override void End()
        {
        }
    }

    /// <summary>
    /// Does not support options: Always
    /// </summary>
    sealed class UpdateFileStream : UpdateFile
    {
        // UpdateFileStream does not support UpdateFile::Always
        public UpdateFileStream( UpdateInfoStream parent, uint crc, uint size, string filename, List<string> options )
            : base( parent, crc, size, filename, options )
        {
        }

        public override bool Download( UpdaterFileProgress progress, UpdaterFileSave save, UpdaterFileComplete complete )
        {
            UpdateInfoStream parent = (UpdateInfoStream)this.Parent;

            parent.Send( "Get\n" );
            parent.Send( this.FileName.Replace( '/', '\\' ) + "\n" );
            string catch_ = parent.ReadLine(); // Catch/Catchpack

            uint size = parent.ReadUint();

            if( this.Parent.InfoVersion == 1 || this.Size == 0 )
                this.Parent.Parent.SendProgress( FOUpdaterProgress.FileMaximum, size );

            byte[] data = new byte[size];

            long lastPercent = 0;
            long readed = 0;
            while( readed != size )
            {
                if( this.Parent.Parent.Cancelled )
                    return (false);

                if( parent.Client == null || !parent.Client.Connected )
                    return (false);

                int input = parent.Client.GetStream().ReadByte();
                if( input == -1 )
                    continue;

                data[readed++] = (byte)input;
                long percent = (readed * 100) / size;
                if( percent > lastPercent )
                {
                    progress( this, readed, (long)size, (uint)percent );
                    lastPercent = percent;
                    Thread.Sleep( 50 );
                }
            }

            if( catch_ == "Catchpack" || this.Packed )
            {
                byte[] saveData = ZlibStream.UncompressBuffer( data );
                data = saveData;
            }

            save( this );
            File.WriteAllBytes( this.FileName, data );

            complete( this );
            return (true);
        }
    }

    sealed class UpdateFileHTTP : UpdateFile
    {
        private long lastPercent = 0;
        private UpdaterFileProgress progress = null;
        private UpdaterFileSave save = null;
        private UpdaterFileComplete complete = null;

        public string URL
        {
            get { return (((UpdateInfoHTTP)this.Parent).RootAddress + this.FileName); }
        }

        public UpdateFileHTTP( UpdateInfoHTTP parent, uint crc, uint size, string filename, List<string> options )
            : base( (UpdateInfo)parent, crc, size, filename.Replace( "%20", " " ), options )
        {
        }

        public override bool Download( UpdaterFileProgress progress, UpdaterFileSave save, UpdaterFileComplete complete )
        {
            this.progress = progress;
            this.save = save;
            this.complete = complete;

            WebClient download = new WebClient();

            download.DownloadProgressChanged += new DownloadProgressChangedEventHandler( download_Progress );
            download.DownloadDataCompleted += new DownloadDataCompletedEventHandler( download_Complete );

            download.Headers[HttpRequestHeader.UserAgent] = "FOUpdater/" + FOUpdaterClient.VersionString;

            try
            {
                download.BaseAddress = this.URL;
                download.DownloadDataAsync( new Uri( ((UpdateInfoHTTP)this.Parent).RootAddress + this.FileName ), this.FileName );
            }
            catch
            {
                return (false);
            }

            while( download.IsBusy )
            {
                if( this.Parent.Parent.Cancelled )
                {
                    download.CancelAsync();
                }
                Thread.Sleep( 100 );
            }

            // should be enough to make sure that UpdaterFileComplete callback finished
            Thread.Sleep( 500 );

            return (true);
        }

        object locker = new object();
        void download_Progress( object sender, DownloadProgressChangedEventArgs e )
        {
            lock( locker )
            {
                if( this.Parent.Parent.Cancelled )
                    return;

                long percent = e.ProgressPercentage;

                if( percent > lastPercent )
                {
                    this.progress( this, e.BytesReceived, e.TotalBytesToReceive, (uint)percent );
                    lastPercent = percent;
                }
            }
        }

        void download_Complete( object sender, DownloadDataCompletedEventArgs e )
        {
            lock( locker )
            {
                if( this.Parent.Parent.Cancelled )
                    return;

                this.save( this );
                File.WriteAllBytes( this.FileName, e.Result );

                do { } while( !File.Exists( this.FileName ) );

                this.complete( this );
            }
        }
    }

    /// <summary>
    /// TODO
    /// </summary>
    sealed class UpdateFileFTP : UpdateFile
    {
        public UpdateFileFTP( UpdateInfoFTP parent, uint crc, uint size, string filename, List<string> options )
            : base( (UpdateInfo)parent, crc, size, filename, options )
        {
            throw new NotImplementedException( "UpdateFileFTP" );
        }
    }

    #endregion
}
