using System;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Voice;

namespace VOIP
{

	public class Form1 : System.Windows.Forms.Form
	{

		#region variables
		private Socket socket;
		private Thread thread;
		private bool connected = false;
		private System.Windows.Forms.Label label1;
		private Button button3;
		private Button button4;
		private System.Windows.Forms.TextBox Peer_IP;
		private System.ComponentModel.Container components = null;
		#endregion

		#region Form1()
		public Form1()
		{

			InitializeComponent();
			socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			thread = new Thread(new ThreadStart(Voice_In));
		}
		#endregion

		#region For Desginer
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.Peer_IP = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.button3 = new System.Windows.Forms.Button();
			this.button4 = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// Peer_IP
			// 
			this.Peer_IP.Location = new System.Drawing.Point(120, 8);
			this.Peer_IP.Name = "Peer_IP";
			this.Peer_IP.Size = new System.Drawing.Size(136, 20);
			this.Peer_IP.TabIndex = 4;
			this.Peer_IP.Text = "10.0.0.10";
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(16, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(96, 16);
			this.label1.TabIndex = 5;
			this.label1.Text = "Peer Address ";
			// 
			// button3
			// 
			this.button3.Location = new System.Drawing.Point(24, 40);
			this.button3.Name = "button3";
			this.button3.Size = new System.Drawing.Size(112, 27);
			this.button3.TabIndex = 10;
			this.button3.Text = "Start";
			this.button3.Click += new System.EventHandler(this.button3_Click);
			// 
			// button4
			// 
			this.button4.Location = new System.Drawing.Point(160, 40);
			this.button4.Name = "button4";
			this.button4.Size = new System.Drawing.Size(104, 27);
			this.button4.TabIndex = 11;
			this.button4.Text = "Stop";
			this.button4.Click += new System.EventHandler(this.button4_Click);
			// 
			// Form1
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.AutoScroll = true;
			this.ClientSize = new System.Drawing.Size(282, 80);
			this.Controls.Add(this.button4);
			this.Controls.Add(this.button3);
			this.Controls.Add(this.Peer_IP);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.Name = "Form1";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Peer 1 Full Duplex Voice Chat";
			this.Closing += new System.ComponentModel.CancelEventHandler(this.Form1_Closing_1);
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		/// 

		[STAThread]
		static void Main()
		{
			Application.Run(new Form1());
		}
		#endregion



		#region Voice_In()
		private void Voice_In()
		{
			byte[] br;
			socket.Bind(new IPEndPoint(IPAddress.Any, 5020));
			while (true)
			{
				br = new byte[16384];
				socket.Receive(br);
				m_Fifo.Write(br, 0, br.Length);
			}
		}
		#endregion
		#region Voice_Out()
		
		private void Voice_Out(IntPtr data, int size)
		{
			//for Recorder
			if (m_RecBuffer == null || m_RecBuffer.Length < size)
				m_RecBuffer = new byte[size];
			System.Runtime.InteropServices.Marshal.Copy(data, m_RecBuffer, 0, size);
			//Microphone ==> data ==> m_RecBuffer ==> m_Fifo
			socket.SendTo(m_RecBuffer, new IPEndPoint(IPAddress.Parse(Peer_IP.Text),5030));
		}  
		
		#endregion


        
		//********************************************************************************//
		private WaveOutPlayer m_Player;
		private WaveInRecorder m_Recorder;
		private FifoStream m_Fifo = new FifoStream();

		private byte[] m_PlayBuffer;
		private byte[] m_RecBuffer;


		private void button3_Click(object sender, EventArgs e)
		{
			if (connected == false)
			{
				thread.Start();
				connected = true;
			}

			Start();
		}

		private void button4_Click(object sender, EventArgs e)
		{
			Stop();
		}

		private void Start()
		{
			Stop();
			try
			{
				WaveFormat fmt = new WaveFormat(44100, 16, 2);
				m_Player = new WaveOutPlayer(-1, fmt, 16384, 3, new BufferFillEventHandler(Filler));
				m_Recorder = new WaveInRecorder(-1, fmt, 16384, 3, new BufferDoneEventHandler(Voice_Out));
			}
			catch
			{
				Stop();
				throw;
			}
		}

		private void Stop()
		{
			if (m_Player != null)
				try
				{
					m_Player.Dispose();
				}
				finally
				{
					m_Player = null;
				}
			if (m_Recorder != null)
				try
				{
					m_Recorder.Dispose();
				}
				finally
				{
					m_Recorder = null;
				}
			m_Fifo.Flush(); // clear all pending data
		}

		private void Filler(IntPtr data, int size)
		{
			if (m_PlayBuffer == null || m_PlayBuffer.Length < size)
				m_PlayBuffer = new byte[size];
			if (m_Fifo.Length >= size)
				m_Fifo.Read(m_PlayBuffer, 0, size);
			else
				for (int i = 0; i < m_PlayBuffer.Length; i++)
					m_PlayBuffer[i] = 0;
			System.Runtime.InteropServices.Marshal.Copy(m_PlayBuffer, 0, data, size);
			// m_Fifo ==> m_PlayBuffer==> data ==> Speakers
		}

		private void Form1_Closing_1(object sender, System.ComponentModel.CancelEventArgs e)
		{
			thread.Abort();
			socket.Close();
			Stop();
		}

      
	}
}
