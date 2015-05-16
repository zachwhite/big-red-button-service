﻿using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using log4net;


namespace BigRedButtonService
{
	public class ButtonController
	{

		#region Fields
		private static ILog logger = LogManager.GetLogger(typeof (ButtonController));

		private ButtonConfig buttonConfig;
		private ButtonMonitor buttonMonitor;
		private HttpClient httpClient;
		#endregion


		#region Constructors
		public ButtonController()
		{
			this.httpClient = new HttpClient();
			this.buttonConfig = new ButtonConfig();
		}
		#endregion


		#region Properties
		public ButtonConfig ButtonConfig
		{
			get { return this.buttonConfig; }
		}


		public ButtonMonitor ButtonMonitor
		{
			get { return this.buttonMonitor; }
		}

		public HttpClient HttpClient
		{
			get { return this.httpClient; }
		}
		#endregion


		/// <summary>
		/// Start monitoring button, if available.
		/// If not available, try again in 10 seconds.
		/// </summary>
		public void StartMonitor()
		{
			// Device found, start listening.
			try
			{
				this.buttonMonitor = new ButtonMonitor();

				// Register events.
				this.buttonMonitor.ButtonPressed += 
					() => ExecuteButtonCommand(this.buttonConfig.ButtonPressedCommand);
				this.buttonMonitor.LidClosed +=
					() => ExecuteButtonCommand(this.buttonConfig.LidClosedCommand);
				this.buttonMonitor.LidOpened +=
					() => ExecuteButtonCommand(this.buttonConfig.LidOpenedCommand);

				// Start background thread listening for button state changes.
				this.buttonMonitor.Start();
				logger.Info("Monitoring Big Red Button device."); 
			}

			// Device not found, try again in 10 seconds.
			catch (InvalidOperationException e)
			{
				logger.Warn("Big Red Button device not found. Will try again in 10 seconds.");
				Thread.Sleep(10000);
				StartMonitor();
			}

			// Some other problem with device.
			catch (Exception e)
			{
				logger.Error(e.Message);
			}
		}


		public void StopMonitor()
		{
			this.buttonMonitor.Stop();
			logger.Info("Stopped monitoring Big Red Button device.");
		}


		#region Event Handlers
		/// <summary>
		/// Fired when button is pressed.
		/// </summary>
		private void ExecuteButtonCommand(ButtonCommand buttonCommand)
		{
			//logger.Info(buttonCommand.Type + "-->" + buttonCommand.Command);
			
			// Command line command.
			if (buttonCommand.Type == ButtonCommand.CommandType.CommandLine)
			{
				Task.Run(() =>
				{
					try
					{
						System.Diagnostics.Process.Start(buttonCommand.Command);
					}
					catch
					{
						logger.Error("Could execute shell command.");
					}
				});
			}

			// HTTP request.
			else if (buttonCommand.Type == ButtonCommand.CommandType.HttpRequest)
			{
				
				Task.Run(() =>
				{
					try
					{
						// http://www.remotesoundboard.com/scripts/sb_poll.php?cmd=play&target=!!!RND&channel=default
						this.httpClient.GetStringAsync(buttonCommand.Command);
					}
					catch
					{
						logger.Error("Couldn't send HTTP request.");
					}
				});
			}
		}
		#endregion

	}
}