using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using WeifenLuo.WinFormsUI.Docking;
using LineCounter.Resources;
using PluginCore.Localization;
using PluginCore.Utilities;
using PluginCore.Managers;
using PluginCore.Helpers;
using PluginCore;
using System.Collections.Generic;

namespace LineCounter
{
	public class PluginMain : IPlugin
	{
        private String pluginName = "LineCounter";
        //private String pluginGuid = "42ac7fab-421b-1f38-a985-72a03534f731";
		private String pluginGuid = "E6E47CEA-5F9A-4719-918D-AFBB881216D9";
	     private String pluginHelp = "www.flashdevelop.org/community/";
        private String pluginDesc = "Line Counter Plugin to count source files.";
        private String pluginAuth = "Baris Soner Usakli";
        private String settingFilename;
        private Settings settingObject;
        private DockContent pluginPanel;
        private PluginUI pluginUI;
        private Image pluginImage;

        // extensions to get
        static readonly List<string> extensions = new List<string> { "*.as", "*.hx", "*.mxml", "*.html", "*.js", "*.php", "*.css", "*.xml" };
        

		private uint grandTotalLineCount = 0;
		private uint grandTotalSource = 0;
		private uint grandTotalComments = 0;
		private uint grandTotalBlank = 0;
		private uint grandTotalFileCount = 0;


		private uint pathTotalLineCount = 0;
		private uint pathTotalSource = 0;
		private uint pathTotalComments = 0;
		private uint pathTotalBlank = 0;
		private uint pathTotalFileCount = 0;

	    #region Required Properties

        /// <summary>
        /// Api level of the plugin
        /// </summary>
        public Int32 Api
        {
            get { return 1; }
        }

        /// <summary>
        /// Name of the plugin
        /// </summary> 
        public String Name
		{
			get { return this.pluginName; }
		}

        /// <summary>
        /// GUID of the plugin
        /// </summary>
        public String Guid
		{
			get { return this.pluginGuid; }
		}

        /// <summary>
        /// Author of the plugin
        /// </summary> 
        public String Author
		{
			get { return this.pluginAuth; }
		}

        /// <summary>
        /// Description of the plugin
        /// </summary> 
        public String Description
		{
			get { return this.pluginDesc; }
		}

        /// <summary>
        /// Web address for help
        /// </summary> 
        public String Help
		{
			get { return this.pluginHelp; }
		}

        /// <summary>
        /// Object that contains the settings
        /// </summary>
        [Browsable(false)]
        public Object Settings
        {
            get { return this.settingObject; }
        }
		
		#endregion
		
		#region Required Methods
		
		/// <summary>
		/// Initializes the plugin
		/// </summary>
		public void Initialize()
		{
            this.InitBasics();
            this.LoadSettings();
            this.AddEventHandlers();
            this.InitLocalization();
            this.CreatePluginPanel();
            this.CreateMenuItem();
        }
		
		/// <summary>
		/// Disposes the plugin
		/// </summary>
		public void Dispose()
		{
            this.SaveSettings();
		}
		
		/// <summary>
		/// Handles the incoming events
		/// </summary>
		public void HandleEvent(Object sender, NotifyEvent e, HandlingPriority prority)
		{
            switch (e.Type)
            {
                // Catches FileSwitch event and displays the filename it in the PluginUI.
                case EventType.FileSwitch:
                    string fileName = PluginBase.MainForm.CurrentDocument.FileName;
                    //pluginUI.Output.Text += "WORKING DIR "+PluginBase.MainForm.WorkingDirectory;
                    
                    //pluginUI.Output.Text += fileName + "\r\n";
                    //TraceManager.Add("Switched to " + fileName); // tracing to output panel
                    break;

                // Catches Project change event and display the active project path
                case EventType.Command:
                    string cmd = (e as DataEvent).Action;
                    if (cmd == "ProjectManager.Project")
                    {
                        IProject project = PluginBase.CurrentProject;
                       /* if (project == null)
                            pluginUI.Output.Text += "Project closed.\r\n";
                        else
                            pluginUI.Output.Text += "Project open: " + project.ProjectPath + "\r\n";*/
                    }
                    break;
            }
		}
		
		#endregion

        #region Custom Methods


        public void countLines()
        {
            string[] srcPaths;
            IProject project = PluginBase.CurrentProject;
            if (project == null)
            {
                srcPaths = new string[1];
                srcPaths[0] = PluginBase.MainForm.WorkingDirectory;
            }
            else
            {
                srcPaths = project.SourcePaths;
            }
			
			pluginUI.DataGridFiles.Columns[2].ValueType = 
			pluginUI.DataGridFiles.Columns[3].ValueType = 
			pluginUI.DataGridFiles.Columns[4].ValueType = 
			pluginUI.DataGridFiles.Columns[5].ValueType = typeof(uint);

			pluginUI.DataGridPaths.Columns[1].ValueType =
			pluginUI.DataGridPaths.Columns[2].ValueType =
			pluginUI.DataGridPaths.Columns[3].ValueType =
			pluginUI.DataGridPaths.Columns[4].ValueType = 
			pluginUI.DataGridPaths.Columns[5].ValueType = typeof(uint);

			
            //if (true)
            //{    
				pluginUI.DataGridFiles.Rows.Clear();
				pluginUI.DataGridPaths.Rows.Clear();

				grandTotalBlank = 0;
				grandTotalComments = 0;
				grandTotalSource = 0;
				grandTotalLineCount = 0;
				grandTotalFileCount = 0;


				foreach (var item in srcPaths)
				{

					pathTotalLineCount = 0;
					pathTotalSource = 0;
					pathTotalComments = 0;
					pathTotalBlank = 0;
					pathTotalFileCount = 0;
					String path = item;

					foreach (var ext in extensions)
					{
						

						calculateExtensionLines(path, ext);
					}

					grandTotalBlank += pathTotalBlank;
					grandTotalComments += pathTotalComments;
					grandTotalLineCount += pathTotalLineCount;
					grandTotalSource += pathTotalSource;
					grandTotalFileCount += pathTotalFileCount;


					object[] pathRow = { item, 
								   pathTotalFileCount, 
								   pathTotalLineCount,
								   pathTotalSource,
								   pathTotalComments,
								   pathTotalBlank};

					pluginUI.DataGridPaths.Rows.Add(pathRow);

				}
				
				object[] row = { "TOTAL", 
								   grandTotalFileCount, 
								   grandTotalLineCount,
								   grandTotalSource,
								   grandTotalComments,
								   grandTotalBlank };
				
				pluginUI.DataGridPaths.Rows.Add(row);
           // }
		
        }

        protected void calculateExtensionLines(String path,String extension)
        {
			
			string[] filePaths = Directory.GetFiles(@path, extension, SearchOption.AllDirectories);

			pluginUI.ProgressBar.Value = 1;
			pluginUI.ProgressBar.Minimum = 1;
			pluginUI.ProgressBar.Step = 1;
			pluginUI.ProgressBar.Maximum = filePaths.Length+1;

			foreach (var file in filePaths)
            {
                
				countFile(file,extension);
				pluginUI.ProgressBar.Value ++;
				pluginUI.ProgressBar.Invalidate();
				
            }
			
			
        }

        protected void countFile(String file,String extension)
        {
			uint fileTotalLineCount = 0;
			uint fileCodeCount = 0;
			uint fileCommentCount = 0;
			uint fileBlankCount = 0;

            Boolean isInComment = false;//是否在注释块内
			
			String[] fileContents = File.ReadAllLines(file);
            if (fileContents.Length>0)
            {
				fileTotalLineCount = (uint)fileContents.Length;
            }
            foreach (var line in fileContents)
            {
				if (line.Trim().Length == 0)
					fileBlankCount++;
				else
				{
                    String trimmed = line.Trim();

                    if (isInComment)
                        fileCommentCount++;//在注释块内 ++


                    if (trimmed.StartsWith("/*"))
                    {
                        isInComment = true;
                        fileCommentCount++;
                    }

                    if (trimmed.EndsWith("*/"))
                    {
                        isInComment = false;
                        continue;
                    }

                    if (isInComment) continue;
                 
					if (trimmed.StartsWith("//"))
						fileCommentCount++;
					else
						fileCodeCount++;
				}				
            }
			string filename = file.Substring(file.LastIndexOf("\\")+1);
			object[] row = {filename,
									extension,
								   fileTotalLineCount,
									fileCodeCount,
									fileCommentCount,
									fileBlankCount,
									file};

			pluginUI.DataGridFiles.Rows.Add(row);

			pathTotalBlank += fileBlankCount;
			pathTotalComments += fileCommentCount;
			pathTotalLineCount += fileTotalLineCount;
			pathTotalSource += fileCodeCount;
			pathTotalFileCount++;
		
        }

        /// <summary>
        /// Initializes important variables
        /// </summary>
        public void InitBasics()
        {
            String dataPath = Path.Combine(PathHelper.DataDir, "LineCounter");
            if (!Directory.Exists(dataPath)) Directory.CreateDirectory(dataPath);
            this.settingFilename = Path.Combine(dataPath, "Settings.fdb");
            this.pluginImage = PluginBase.MainForm.FindImage("100");
        }

        /// <summary>
        /// Adds the required event handlers
        /// </summary> 
        public void AddEventHandlers()
        {
            // Set events you want to listen (combine as flags)
            EventManager.AddEventHandler(this, EventType.FileSwitch | EventType.Command);
        }

        /// <summary>
        /// Initializes the localization of the plugin
        /// </summary>
        public void InitLocalization()
        {
            LocaleVersion locale = PluginBase.MainForm.Settings.LocaleVersion;
            switch (locale)
            {
                /*
                case LocaleVersion.fi_FI : 
                    // We have Finnish available... or not. :)
                    LocaleHelper.Initialize(LocaleVersion.fi_FI);
                    break;
                */
                default : 
                    // Plugins should default to English...
                    LocaleHelper.Initialize(LocaleVersion.en_US);
                    break;
            }
            this.pluginDesc = LocaleHelper.GetString("Info.Description");
        }

        /// <summary>
        /// Creates a menu item for the plugin and adds a ignored key
        /// </summary>
        public void CreateMenuItem()
        {
            ToolStripMenuItem viewMenu = (ToolStripMenuItem)PluginBase.MainForm.FindMenuItem("ViewMenu");
            viewMenu.DropDownItems.Add(new ToolStripMenuItem(LocaleHelper.GetString("Label.ViewMenuItem"), this.pluginImage, new EventHandler(this.OpenPanel), this.settingObject.SampleShortcut));
            PluginBase.MainForm.IgnoredKeys.Add(this.settingObject.SampleShortcut);
        }

        /// <summary>
        /// Creates a plugin panel for the plugin
        /// </summary>
        public void CreatePluginPanel()
        {
            this.pluginUI = new PluginUI(this);
            this.pluginUI.Text = LocaleHelper.GetString("Title.PluginPanel");
            this.pluginPanel = PluginBase.MainForm.CreateDockablePanel(this.pluginUI, this.pluginGuid, this.pluginImage, DockState.DockRight);
        }

        /// <summary>
        /// Loads the plugin settings
        /// </summary>
        public void LoadSettings()
        {
            this.settingObject = new Settings();
            if (!File.Exists(this.settingFilename)) this.SaveSettings();
            else
            {
                Object obj = ObjectSerializer.Deserialize(this.settingFilename, this.settingObject);
                this.settingObject = (Settings)obj;
            }
        }

        /// <summary>
        /// Saves the plugin settings
        /// </summary>
        public void SaveSettings()
        {
            ObjectSerializer.Serialize(this.settingFilename, this.settingObject);
        }

        /// <summary>
        /// Opens the plugin panel if closed
        /// </summary>
        public void OpenPanel(Object sender, System.EventArgs e)
        {
            this.pluginPanel.Show();
        }

		#endregion

	}
	
}
