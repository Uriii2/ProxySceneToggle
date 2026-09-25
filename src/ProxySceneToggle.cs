// ProxyToggle - переключатель сценария системного прокси Windows из трея

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading; 
using Microsoft.Win32;


class ProxyToggleApp : ApplicationContext{
    
    const string REG_PATH = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings\Connections";	

    NotifyIcon tray;
    MenuItem statusItem;
    MenuItem toggleItem;

    [DllImport("wininet.dll", SetLastError = true)]
    static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
    const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
    const int INTERNET_OPTION_REFRESH = 37;
	
	private System.Threading.Timer _timer;
	private readonly SynchronizationContext _uiContext;	
	private bool ManualClickOff ;

  
	public ProxyToggleApp()
    {
        // Захватываем контекст UI-потока
        _uiContext = SynchronizationContext.Current
            ?? new WindowsFormsSynchronizationContext();
		
		tray = new NotifyIcon();
        tray.Visible = true;
        tray.MouseClick += OnTrayClick;
		ManualClickOff = false;

        statusItem = new MenuItem("...") { Enabled = false };
        toggleItem = new MenuItem("Переключить", (s, e) => Toggle());
        var exitItem = new MenuItem("Выход", (s, e) => ExitApp());
        tray.ContextMenu = new ContextMenu(new[] { statusItem, toggleItem, new MenuItem("-"), exitItem });
        UpdateUI();
		StartPeriodicCheck(); 
    }

    
	void OnTrayClick(object sender, MouseEventArgs e)
    {
         if (e.Button == MouseButtons.Left) Toggle();
	}



    public static byte[] GetCurrentBytes()
    {
        string subKey = REG_PATH; // Путь к ключу
		string valueName = "DefaultConnectionSettings";          
		
		// Открываем ключ для записи (HKEY_CURRENT_USER не требует админ-прав)
		using (RegistryKey key = Registry.CurrentUser.CreateSubKey(subKey, writable: true))
		{
			if (key == null)
			{
				Console.WriteLine("Не удалось создать/открыть ключ.");
			}
			
			// Получаем текущее значение типа REG_BINARY
			object rawValue = key.GetValue(valueName);
			byte[] currentBytes = rawValue as byte[];
			
			return currentBytes;
        }
    }

       

	void Toggle()
	{		
		string subKey = REG_PATH; // Путь к ключу
		string valueName = "DefaultConnectionSettings";
		int byteIndex = 8;    // Индекс байта, который нужно изменить (0-based)						
				
		// Открываем ключ для записи (HKEY_CURRENT_USER не требует админ-прав)
		using (RegistryKey key = Registry.CurrentUser.CreateSubKey(subKey, writable: true))
		{
						
			if (key == null)
			{
				Console.WriteLine("Не удалось создать/открыть ключ.");
				return;
			}
			
			// Получаем текущее значение типа REG_BINARY
			object rawValue = key.GetValue(valueName);
			byte[] currentBytes = rawValue as byte[];
		
			if (currentBytes == null)
			{
				Console.WriteLine("Значение существует, но не является массивом байтов (REG_BINARY).");
				return;
			}
			// Теперь можно безопасно использовать индексацию
			if (byteIndex >= 0 && byteIndex < currentBytes.Length)
			{				
				string bitValue = currentBytes[8].ToString();
						
				//Значение 9-го бита "7" - означает, что переключатель опций Сценария прокси включен. Другое значение означает, что опция отключена.
				if (bitValue == "7")  
				{
					currentBytes[byteIndex] = 0x03;
					ManualClickOff = true; //вручную выключена
				}
				else 
				{
					currentBytes[byteIndex] = 0x07;
					ManualClickOff = false; 
				}				
				key.SetValue(valueName, currentBytes, RegistryValueKind.Binary);				
			}
			else
			{
				Console.WriteLine("Индекс байта выходит за границы массива.");
			}
			// Записываем обновлённое значение обратно
			 key.SetValue(valueName, rawValue, RegistryValueKind.Binary);			
		}	

		// Сообщаем системе, что настройки изменились (применяется сразу, без перезагрузки)
        InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr. Zero, 0);
        InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);

        UpdateUI();
		
			//Раскоментировать, если нужно всплывающее окно из трея
			//string bitValue2 = GetCurrentBytes()[8].ToString();			
			// bool enable = bitValue2 == "7";
			// tray.ShowBalloonTip(1500, "ProxyToggle",
				// enable ? "Сценарий Proxy.pac ВКЛЮЧЁН"  : "Сценарий Proxy.pac ВЫКЛ",
				// enable ? ToolTipIcon.Info : ToolTipIcon.None);	
	}		


    public void UpdateUI()
    {
	string bitValue = GetCurrentBytes()[8].ToString();			
	bool on = bitValue == "7";	

	tray.Icon = MakeIcon(on);
	tray.Text = on ? "Сценарий Proxy.pac:   ВКЛ" : "Сценарий Proxy.pac:   выкл";
	statusItem.Text = on ? "● Сценарий Proxy.pac   включён" : "Сценарий Proxy.pac   выключен";
	toggleItem.Text = on ? "Выключить Proxy.pac" : "Включить Proxy.pac";
	
	//если вручную не выключалось, но по каким-то причинам система сценарий отключила, то заново включаем. 
	if (ManualClickOff == false && on == false) 
		{
			Toggle();
		}
    }

    // Рисуем иконку: зелёный кружок = вкл, серый = выкл
    Icon MakeIcon(bool on)
    {
        using (var bmp = new Bitmap(32, 32))
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            Color main = on ? Color.FromArgb(46, 204, 113) : Color.FromArgb(130, 130, 130);
            using (var brush = new SolidBrush(main))
                g.FillEllipse(brush, 3, 3, 26, 26);
            using (var pen = new Pen(Color.White, 3))
            {
                if (on)
                {
                    // галочка
                    g.DrawLines(pen, new[] { new Point(9, 16), new Point(14, 21), new Point(23, 11) });
                }
                else
                {
                    // вертикальная чёрточка (символ питания)
                    g.DrawLine(pen, 16, 9, 16, 17);
                }
            }
            IntPtr h = bmp.GetHicon();
            return Icon.FromHandle(h);
        }
    }

    void ExitApp()
    {
        tray.Visible = false;
        tray.Dispose();
        Application.Exit();
    }
	
	
	void StartPeriodicCheck()
    {
        int intervalMs = 10000; //Таймер каждые 10 секунд. Если что, меняет цвет иконки.
        _timer = new System.Threading.Timer(
            callback: _ => 			
			{
                try
                {
                    Upd();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            },			
            state: null,
            dueTime: 0,
            period: intervalMs
        );
    }
	
	
		private void Upd()
    {
        _uiContext.Post(_ =>
        {
            UpdateUI();
        }, null);
	}
	
	
	protected override void Dispose(bool disposing)
    {
        if (disposing)
		{
			if (_timer != null) _timer.Dispose();
			if (tray != null) tray.Dispose();
		}
        base.Dispose(disposing);
    }
	
	
	[STAThread]
    static void Main()
    {
        // Не даём запустить вторую копию
        bool created;
        using (var mutex = new System.Threading.Mutex(true, "ProxyToggle_SingleInstance", out created))
        {
            if (!created) return;
            Application.EnableVisualStyles();
            Application.Run(new ProxyToggleApp());			
        }		
    }
	
}	

		
