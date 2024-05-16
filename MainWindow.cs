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
		/// 在主线程上显示东西
		/// </summary>
		void AppendText(string dataToAppend)
		{
			textBox2.Text = dataToAppend + Environment.NewLine + textBox2.Text;
			transParentTimer = 0;
		}

		int transParentTimer = 0;

		//**************保存设置相关**********************
		void SaveSettings()
		{
			settings.left = Left; // 窗口左上角的X坐标
			settings.top = Top; // 窗口左上角的Y坐标
			settings.width = Width; // 窗口宽度
			settings.height = Height; // 窗口高度
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
		//**************加载设置相关**********************
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
			Left = settings.left; // 窗口左上角的X坐标
			Top = settings.top; // 窗口左上角的Y坐标
			Width = settings.width; // 窗口宽度
			Height = settings.height; // 窗口高度
		}


		//**************拖动**********************
		Point point; //鼠标按下时的点
		bool isMoving = false;//标识是否拖动
		private void MainWindow_MouseDown(object sender, MouseEventArgs e)
		{
			point = e.Location;
			isMoving = true;//标识是否拖动
		}

		private void MainWindow_MouseUp(object sender, MouseEventArgs e)
		{
			isMoving = false;//标识是否拖动
		}
		private void MainWindow_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left && isMoving)
			{
				Point pNew = new Point(e.Location.X - point.X, e.Location.Y - point.Y);
				Location = new Point(Location.X + pNew.X, Location.Y + pNew.Y);
			}
		}


		//**************其余事件**********************
		private void Form1_Load(object sender, EventArgs e)
		{
			LoadSettings();
			textBox1.Focus();
			myNetThreads = new MyWebThreads(settings.targetHost, settings.targetPort, settings.roomName, AppendText, this);
			timer1.Start();//开启计时器
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
				e.Handled = true; // 防止回车键插入换行符
				ProcessText(textBox1.Text);
				textBox1.Text = "";
			}
		}
		[DllImport("user32.dll")]
		private static extern IntPtr GetForegroundWindow();

		private void timer1_Tick(object sender, EventArgs e)
		{
			IntPtr foregroundWindowHandle = GetForegroundWindow();
			// 比较当前活动窗口句柄与程序主窗口句柄
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
	ReceivedTextDelegate rtd;// 定义回调函数，内部将通过Invoke调用这个
	private readonly BlockingCollection<string> netQueue_WillSendMessages = new BlockingCollection<string>();
	private Thread? netStartThread = null;
	private Socket? usedSocket_T = null;
	bool myIsLooping = true;
	// 多线程执行的函数-发送数据
	void SendFunction_Thread(Socket socket)
	{
		try
		{
			//
			while (myIsLooping)
			{
				//定义一个函数
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
				//发送数据
				if (netQueue_WillSendMessages.TryTake(out string? stringToSend, TimeSpan.FromSeconds(0.5)))
				{
					if (stringToSend != null)
					{
						byte[] dataToSend = Encoding.UTF8.GetBytes(stringToSend);
						byte[] lengthBytes = BitConverter.GetBytes((Int32)dataToSend.Length);
						if (!BitConverter.IsLittleEndian)// 确保是小端序
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
	// 多线程执行的函数
	void RecvFunction_Thread(string host, int port, string roomName)
	{
		// 新建socket
		usedSocket_T = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		try
		{
			// 连接网络
			usedSocket_T.Connect(host, port);
			RenewUI_ByThreadigInvoke($"成功连接到{host}:{port}");
			// 在发送缓存区前写一个房间名字
			ConcurrentQueue<string> tempQueue = new ConcurrentQueue<string>();
			tempQueue.Enqueue(roomName);
			string? item;
			while (netQueue_WillSendMessages.TryTake(out item))// 将原集合中的所有元素移动到临时队列
			{
				tempQueue.Enqueue(item);
			}
			while (tempQueue.TryDequeue(out item))// 塞回去
			{
				netQueue_WillSendMessages.Add(item);
			}
			new Thread(() => SendFunction_Thread(usedSocket_T)).Start();
			RenewUI_ByThreadigInvoke($"开启发送线程");
			// 定义接收固定数量的函数
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
				// 先解头再收数据
				int intValue = BitConverter.ToInt32(RecvNums(usedSocket_T, 4), 0);
				string recvString = Encoding.UTF8.GetString(RecvNums(usedSocket_T, intValue));
				RenewUI_ByThreadigInvoke($"{recvString}");
			}
		}
		catch (Exception e)
		{
			RenewUI_ByThreadigInvoke($"失败连接到{host}:{port}{e.Message}");
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
		// 开启一个线程就结束了
		rtd = function;
		if (netStartThread == null)
		{
			netStartThread = new Thread(() => RecvFunction_Thread(targetHost, targetPort, roomName));
			netStartThread.Start();
		}
	}
	//多线程调用，发一个invoke
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
	/// 请在结束前执行这个
	/// </summary>
	public void MySafeRelease()
	{
		myIsLooping = false;
		usedSocket_T?.Close();
		usedSocket_T = null;//后续进程通过这个判断是否结束
		netStartThread?.Join();
		netStartThread = null;
	}
}