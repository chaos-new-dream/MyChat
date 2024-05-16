using System.Text.Json;
using System.Text;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Linq;
using System.Threading;
namespace MyChat
{
	public partial class MainWindow : Form
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		MyWebThreads? myNetThreads;


		const string settingsFilePath = "setting.json";
		class Settings
		{
			public string targetHost { get; set; } = "127.0.0.1";
			public int targetPort { get; set; } = 8899;
			public string thisName { get; set; } = "Player";
			public string roomName { get; set; } = "8899";
			public int left { get; set; } = 100;
			public int top { get; set; } = 100;
			public int width { get; set; } = 500;
			public int height { get; set; } = 800;
			public int startFadeTime_ms { get; set; } = 1000;
			public int endFadeTime_ms { get; set; } = 1500;
			public double maxOpacity { get; set; } = 0.8;
			public double minOpacity { get; set; } = 0.1;
		}
		Settings settings = new Settings();


		void ProcessText(String textToProcess)
		{
			switch (textToProcess)
			{
				case "exit":
				case "close":
					break;
				case "start":
				case "connect":
					break;
				default:
					//textBox2.Text = textToProcess;
					myNetThreads?.MySendMessage(settings.thisName + ": " + textToProcess);
					AppendText(textToProcess);
					break;
			}
		}

		/// <summary>
		/// �����߳�����ʾ����
		/// </summary>
		void AppendText(string dataToAppend)
		{
			textBox2.Text = dataToAppend + Environment.NewLine + textBox2.Text;
			transParentTimer = 0;
		}

		int transParentTimer = 0;

		//**************�����������**********************
		void SaveSettings()
		{
			settings.left = Left; // �������Ͻǵ�X����
			settings.top = Top; // �������Ͻǵ�Y����
			settings.width = Width; // ���ڿ��
			settings.height = Height; // ���ڸ߶�
			try
			{
				var options = new JsonSerializerOptions
				{
					WriteIndented = true
				};
				string jsonString = JsonSerializer.Serialize(settings, options);
				File.WriteAllText(settingsFilePath, jsonString, Encoding.UTF8);
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message, "Err");
			}
		}
		//**************�����������**********************
		void LoadSettings()
		{
			try
			{
				string jsonString = File.ReadAllText(settingsFilePath, Encoding.UTF8);
				Settings? trys = JsonSerializer.Deserialize<Settings>(jsonString);
				if (trys != null) settings = trys;
			}
			catch (Exception e)
			{
				textBox2.Text = e.Message;
			}
			Left = settings.left; // �������Ͻǵ�X����
			Top = settings.top; // �������Ͻǵ�Y����
			Width = settings.width; // ���ڿ��
			Height = settings.height; // ���ڸ߶�
		}


		//**************�϶�**********************
		Point point; //��갴��ʱ�ĵ�
		bool isMoving = false;//��ʶ�Ƿ��϶�
		private void MainWindow_MouseDown(object sender, MouseEventArgs e)
		{
			point = e.Location;
			isMoving = true;//��ʶ�Ƿ��϶�
		}

		private void MainWindow_MouseUp(object sender, MouseEventArgs e)
		{
			isMoving = false;//��ʶ�Ƿ��϶�
		}
		private void MainWindow_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left && isMoving)
			{
				Point pNew = new Point(e.Location.X - point.X, e.Location.Y - point.Y);
				Location = new Point(Location.X + pNew.X, Location.Y + pNew.Y);
			}
		}


		//**************�����¼�**********************
		private void Form1_Load(object sender, EventArgs e)
		{
			LoadSettings();
			textBox1.Focus();
			myNetThreads = new MyWebThreads(settings.targetHost, settings.targetPort, settings.roomName, AppendText, this);
			timer1.Start();//������ʱ��
		}
		private void textBox1_TextChanged(object sender, EventArgs e)
		{

		}

		private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
		{
			SaveSettings();
			myNetThreads?.MySafeRelease();
		}

		private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == (char)Keys.Enter)
			{
				e.Handled = true; // ��ֹ�س������뻻�з�
				ProcessText(textBox1.Text);
				textBox1.Text = "";
			}
		}
		[DllImport("user32.dll")]
		private static extern IntPtr GetForegroundWindow();

		private void timer1_Tick(object sender, EventArgs e)
		{
			IntPtr foregroundWindowHandle = GetForegroundWindow();
			// �Ƚϵ�ǰ����ھ������������ھ��
			if (foregroundWindowHandle == this.Handle)
			{
				transParentTimer = 0;
				Opacity = settings.maxOpacity;
			}
			else
			{
				transParentTimer += timer1.Interval;
				if (transParentTimer < settings.startFadeTime_ms)
				{
					Opacity = settings.maxOpacity;
				}
				else if (transParentTimer < settings.endFadeTime_ms)
				{
					double bili = (transParentTimer - settings.startFadeTime_ms);
					bili /= (double)(settings.endFadeTime_ms - settings.startFadeTime_ms);
					Opacity = (1 - bili) * settings.maxOpacity + bili * settings.minOpacity;
				}
				else
				{
					transParentTimer = settings.endFadeTime_ms;
					Opacity = settings.minOpacity;
				}
			}
		}
	}
}














class MyWebThreads
{
	public delegate void ReceivedTextDelegate(string dataToAppend);
	MyChat.MainWindow fatherWindow;
	ReceivedTextDelegate rtd;// ����ص��������ڲ���ͨ��Invoke�������
	private readonly BlockingCollection<string> netQueue_WillSendMessages = new BlockingCollection<string>();
	private Thread? netStartThread = null;
	private Socket? usedSocket_T = null;
	bool myIsLooping = true;
	// ���߳�ִ�еĺ���-��������
	void SendFunction_Thread(Socket socket)
	{
		try
		{
			//
			while (myIsLooping)
			{
				//����һ������
				static void SendAllBytes(byte[] data, Socket s)
				{
					int totalSent = 0;
					while (totalSent < data.Length)
					{
						int sent = s.Send(data, totalSent, data.Length - totalSent, SocketFlags.None);
						if (sent == 0)
						{
							throw new SocketException();
						}
						totalSent += sent;
					}
				}
				//��������
				if (netQueue_WillSendMessages.TryTake(out string? stringToSend, TimeSpan.FromSeconds(0.5)))
				{
					if (stringToSend != null)
					{
						byte[] dataToSend = Encoding.UTF8.GetBytes(stringToSend);
						byte[] lengthBytes = BitConverter.GetBytes((Int32)dataToSend.Length);
						if (!BitConverter.IsLittleEndian)// ȷ����С����
						{
							Array.Reverse(lengthBytes);
						}
						SendAllBytes(lengthBytes, socket);
						SendAllBytes(dataToSend, socket);
					}
				}
			}
		}
		finally
		{
			socket.Close();
		}
		myIsLooping = false;
		//fatherWindow.BeginInvoke((MethodInvoker)delegate { fatherWindow.Close(); });
	}
	// ���߳�ִ�еĺ���
	void RecvFunction_Thread(string host, int port, string roomName)
	{
		// �½�socket
		usedSocket_T = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		try
		{
			// ��������
			usedSocket_T.Connect(host, port);
			RenewUI_ByThreadigInvoke($"�ɹ����ӵ�{host}:{port}");
			// �ڷ��ͻ�����ǰдһ����������
			ConcurrentQueue<string> tempQueue = new ConcurrentQueue<string>();
			tempQueue.Enqueue(roomName);
			string? item;
			while (netQueue_WillSendMessages.TryTake(out item))// ��ԭ�����е�����Ԫ���ƶ�����ʱ����
			{
				tempQueue.Enqueue(item);
			}
			while (tempQueue.TryDequeue(out item))// ����ȥ
			{
				netQueue_WillSendMessages.Add(item);
			}
			new Thread(() => SendFunction_Thread(usedSocket_T)).Start();
			RenewUI_ByThreadigInvoke($"���������߳�");
			// ������չ̶������ĺ���
			static byte[] RecvNums(Socket sock, int n)
			{
				byte[] data = new byte[n];
				int totalReceived = 0;
				while (totalReceived < n)
				{
					int received = sock.Receive(data, totalReceived, n - totalReceived, SocketFlags.None);
					if (received == 0)
					{
						throw new InvalidOperationException("Socket connection broken");
					}
					totalReceived += received;
				}
				return data;
			}
			while (true)
			{
				// �Ƚ�ͷ��������
				int intValue = BitConverter.ToInt32(RecvNums(usedSocket_T, 4), 0);
				string recvString = Encoding.UTF8.GetString(RecvNums(usedSocket_T, intValue));
				RenewUI_ByThreadigInvoke($"{recvString}");
			}
		}
		catch (Exception e)
		{
			RenewUI_ByThreadigInvoke($"ʧ�����ӵ�{host}:{port}{e.Message}");
			usedSocket_T?.Close();
			usedSocket_T = null;
		}
		netStartThread = null;
		myIsLooping = false;
		fatherWindow.BeginInvoke((MethodInvoker)delegate { fatherWindow.Close(); });
	}


	public MyWebThreads(string targetHost, int targetPort, string roomName, ReceivedTextDelegate function, MyChat.MainWindow fatherWindow)
	{
		this.fatherWindow = fatherWindow;
		// ����һ���߳̾ͽ�����
		rtd = function;
		if (netStartThread == null)
		{
			netStartThread = new Thread(() => RecvFunction_Thread(targetHost, targetPort, roomName));
			netStartThread.Start();
		}
	}
	//���̵߳��ã���һ��invoke
	void RenewUI_ByThreadigInvoke(string dataToAppend)
	{
		fatherWindow.BeginInvoke((MethodInvoker)delegate { rtd(dataToAppend); });
	}
	public void MySendMessage(string textToProcess)
	{
		if (myIsLooping)
		{
			netQueue_WillSendMessages.TryAdd(textToProcess);
		}
	}
	/// <summary>
	/// ���ڽ���ǰִ�����
	/// </summary>
	public void MySafeRelease()
	{
		myIsLooping = false;
		usedSocket_T?.Close();
		usedSocket_T = null;//��������ͨ������ж��Ƿ����
		netStartThread?.Join();
		netStartThread = null;
	}
}