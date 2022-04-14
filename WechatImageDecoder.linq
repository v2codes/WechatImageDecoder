<Query Kind="Program">
  <Output>DataGrids</Output>
  <Namespace>Microsoft.Win32</Namespace>
  <Namespace>System.Globalization</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

/*
*	自动判断微信PC客户端加密文件dat后缀格式图片16进制文件头
*	自动计算图片文件解密偏移量并转换为可用的图片文件（jpg|png|gif）
*	可指定具体微信图片缓存本地的日期时间范围进行转换图片提取
*
*	JPG文件头16进制为FFD8FF
*	PNG文件头16进制为89504E47
*	GIF文件头16进制为47494638
*
*	by CCWT923
*	2022-4-14 16:21:35
*/

void Main()
{
	var path = @"D:\Tencent\Cache\wxid_xxxx\FileStorage\Image\Test\";
	var dirInfo = new DirectoryInfo(path);
	var outputPath = Path.Combine(dirInfo.Parent.FullName, $"{dirInfo.Name}-output");
	if (!Directory.Exists(outputPath))
	{
		Directory.CreateDirectory(outputPath);
	}

	var files = GetFileList(path);

	var imageCounter = new ImageTypeCounter();

	foreach (var file in files)
	{
		if (!File.Exists(file))
		{
			continue;
		}
		var imageType = GetImageType(file, out int dencodeValue);
		var decodeData = DecodeImage(file, dencodeValue);
		var newFileName = Path.Combine(outputPath, $"{Path.GetFileNameWithoutExtension(file)}.{imageType.ToString().ToLower()}");
		using (var fs = new FileStream(newFileName, FileMode.OpenOrCreate, FileAccess.Write))
		{
			fs.Write(decodeData);
		}

		switch (imageType)
		{
			case ImageType.JPG:
				imageCounter.JPG++;
				break;
			case ImageType.PNG:
				imageCounter.PNG++;
				break;
			case ImageType.BMP:
				imageCounter.BMP++;
				break;
			case ImageType.GIF:
				imageCounter.GIF++;
				break;
			case ImageType.UNKNOW:
				imageCounter.UNKNOW++;
				break;
		}
	}
	$"图片解码完成：JGP {imageCounter.JPG} 个，PNG {imageCounter.PNG} 个，PNG {imageCounter.PNG} 个，BMP {imageCounter.BMP} 个，GIF {imageCounter.GIF} 个。".Dump();
	$"非图片格式文件：{imageCounter.UNKNOW} 个。".Dump();
}

/// <summary>
/// 解码图片
/// </summary>
/// <param name="fileName">原始文件名</param>
/// <param name="encodeValue">dat 格式文件的第一个字节对相应格式异或后的值，用这个值对每个字节进行异或操作</param>
/// <returns></returns>
private byte[] DecodeImage(string fileName, int encodeValue)
{
	byte[] imgData;
	using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
	{
		imgData = new byte[fs.Length];
		var readLength = 0;
		var totalLen = imgData.Length;
		while (totalLen > 0)
		{
			int n = fs.Read(imgData, readLength, totalLen);
			if (n == 0)
			{
				break;
			}
			readLength += n;
			totalLen -= n;
		}
		//对每个字节进行异或操作
		for (int i = 0; i < imgData.Length; i++)
		{
			imgData[i] = (byte)(imgData[i] ^ encodeValue);
		}
	}
	return imgData;
}

/// <summary>
/// 解析图格式
/// </summary>
/// <param name="fileName"></param>
/// <param name="decodeValue">将 dat 文件的第一个字节和对应格式头部的第一个字节进行异或操作的值，这个值就可以用来解码</param>
/// <param name="imageFormat">图片格式</param>
private ImageType GetImageType(string fileName, out int xorCodeValue)
{
	using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
	{
		// 获取文件前两位
		var data = new byte[2];
		fs.Read(data, 0, 2);
		// 判断图片格式，将图片的前两个字节和对应的文件头进行异或操作
		if ((data[0] ^ (byte)ImageHeaderValue.JpegHeaderValue1) == (data[1] ^ (byte)ImageHeaderValue.JpegHeaderValue2))
		{
			xorCodeValue = data[0] ^ (byte)ImageHeaderValue.JpegHeaderValue1;
			return ImageType.JPG;
		}

		if ((data[0] ^ (byte)ImageHeaderValue.PngHeaderValue1) == (data[1] ^ (byte)ImageHeaderValue.PngHeaderValue2))
		{
			xorCodeValue = data[0] ^ (byte)ImageHeaderValue.PngHeaderValue1;
			return ImageType.PNG;
		}

		if ((data[0] ^ (byte)ImageHeaderValue.GifHeaderValue1) == (data[1] ^ (byte)ImageHeaderValue.GifHeaderValue2))
		{
			xorCodeValue = data[0] ^ (byte)ImageHeaderValue.GifHeaderValue1;
			return ImageType.GIF;
		}

		if ((data[0] ^ (byte)ImageHeaderValue.BmpHeaderValue1) == (data[1] ^ (byte)ImageHeaderValue.BmpHeaderValue2))
		{
			xorCodeValue = data[0] ^ (byte)ImageHeaderValue.BmpHeaderValue1;
			return ImageType.BMP;
		}
		xorCodeValue = 0;
		return ImageType.UNKNOW;
	}
}

/// <summary>
/// 图片类型
/// </summary>
private enum ImageType
{
	JPG,
	PNG,
	BMP,
	GIF,
	UNKNOW
}

/// <summary>
/// 图片文件头前两个字节的值
/// JPG文件头16进制为 0xFFD8FF
/// PNG文件头16进制为 0x89504E47
/// GIF文件头16进制为 0x47494638
/// BMP文件头16进制为 0x424D
/// </summary>
private enum ImageHeaderValue : byte
{
	JpegHeaderValue1 = 0xFF,
	JpegHeaderValue2 = 0xD8,
	PngHeaderValue1 = 0x89,
	PngHeaderValue2 = 0x50,
	GifHeaderValue1 = 0x47,
	GifHeaderValue2 = 0x49,
	BmpHeaderValue1 = 0x42,
	BmpHeaderValue2 = 0x4D
}

/// <summary>
/// 获取文件列表
/// </summary>
/// <param name="path">目标文件夹</param>
private string[] GetFileList(string path)
{
	var files = Directory.GetFiles(path);
	var result = files.OrderByDescending(p => new FileInfo(p).CreationTime).ToArray();
	return result;
}

/// <summary>
/// 解码计数
/// </summary>
struct ImageTypeCounter
{
	public int JPG { get; set; }
	public int PNG { get; set; }
	public int BMP { get; set; }
	public int GIF { get; set; }
	public int UNKNOW { get; set; }
}


#region TODO 自动获取微信缓存目录
public static void DisplayAllProgram()
{
	RegistryKey uninstallKey, programKey;
	uninstallKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
	string[] programKeys = uninstallKey.GetSubKeyNames();
	foreach (string keyName in programKeys)
	{
		programKey = uninstallKey.OpenSubKey(keyName);
		Console.WriteLine(keyName + " , " + programKey.GetValue("DisplayName") + " , " + programKey.GetValue("InstallLocation"));
		programKey.Close();
	}
	uninstallKey.Close();
}

/// <summary>
/// 从注册表中寻找安装路径
/// </summary>
/// <param name="uninstallKeyName">
/// 安装信息的注册表键名
/// 微信：WeChat
/// QQ：{052CFB79-9D62-42E3-8A15-DE66C2C97C3E} 
/// TIM：TIM
/// </param>
/// <returns>安装路径</returns>
public static string FindInstallPathFromRegistry(string uninstallKeyName)
{
	try
	{
		RegistryKey key = Registry.LocalMachine.OpenSubKey($@"Software\Microsoft\Windows\CurrentVersion\Uninstall\{uninstallKeyName}");
		if (key == null)
		{
			return null;
		}
		object installLocation = key.GetValue("InstallLocation");
		key.Close();
		if (installLocation != null && !string.IsNullOrEmpty(installLocation.ToString()))
		{
			return installLocation.ToString();
		}
	}
	catch (Exception e)
	{
		Console.WriteLine(e.Message);
	}
	return null;
}

#endregion