﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Collections;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace FileEncryptSample
{
	public partial class Form1 : Form
	{
		//Encrypt or Decrypt the files list.
		List<string> FileList = new List<string>();
		
		public Form1()
		{
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			textBox1.Text = "ここに暗号化したいテキストファイル（*.txt）を" + Environment.NewLine + "ドラッグ＆ドロップしてください。" + 
				Environment.NewLine + 
				Environment.NewLine +
				"Drag & Drop here to encrypt text files ( *.txt ).";
		}

		//------------------------------------------------------------------------
		// Exit button Click event.
		//------------------------------------------------------------------------
		private void cmdExit_Click(object sender, EventArgs e)
		{
			Close();
		}

		//------------------------------------------------------------------------
		// Form1 Drag Enter event. 
		//------------------------------------------------------------------------
		private void Form1_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop) == true)
			{
				e.Effect = DragDropEffects.Copy;
			}
			else
			{
				e.Effect = DragDropEffects.None;
			}
		}

		//------------------------------------------------------------------------
		// Form1 Drag & Drop event. 
		//------------------------------------------------------------------------
		private void Form1_DragDrop(object sender, DragEventArgs e)
		{

			textBox1.Clear();
			FileList.Clear();

			string[] FileArray = (string[])e.Data.GetData(DataFormats.FileDrop, false);

			foreach (string s in FileArray)
			{
				FileList.Add(s);
				textBox1.AppendText(s + Environment.NewLine);
			}
			
		}

		//------------------------------------------------------------------------
		// Encrypt button Click event. 
		//------------------------------------------------------------------------
		private void cmdEncrypt_Click(object sender, EventArgs e)
		{
			if (FileList.Count == 0)
			{
				//Files is not found to encrypt.
				MessageBox.Show("暗号化するファイルが見つかりません!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}

			bool fEncryptionFile = false;
			foreach (string FilePath in FileList)
			{
				if (String.Compare(Path.GetExtension(FilePath), ".enc", true) == 0)
				{
					fEncryptionFile = true;
					break;
				}
			}

			if (fEncryptionFile == true)
			{
				//Encrypted file exists already. Encryption failed.
				MessageBox.Show("暗号化ファイルがすでに存在します!" + Environment.NewLine + "暗号化に失敗しました。", 
					"Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			else
			{
				foreach (string FilePath in FileList)
				{
					//Encrypt
					FileEncrypt(FilePath, textBox2.Text);
				}
			}
		}

		//------------------------------------------------------------------------
		// Decrypt button Click event. 
		//------------------------------------------------------------------------
		private void cmdDecrypt_Click(object sender, EventArgs e)
		{
			bool fEncryptionFile = false;
			foreach (string FilePath in FileList)
			{
				if (String.Compare(Path.GetExtension(FilePath), ".enc", true) == 0)
				{
					fEncryptionFile = true;
				}
				else
				{
					fEncryptionFile = false;
					break;
				}
			}

			if (fEncryptionFile == true)
			{
				foreach (string FilePath in FileList)
				{
					//Decrypt
					FileDecrypt(FilePath, textBox2.Text);
				}
			}
			else
			{
				//Encrypted files are not found! Decryption failed.
				MessageBox.Show("暗号化されたファイルが見つかりません!" + Environment.NewLine + "復号処理に失敗しいました。", 
					"Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
		}

		//------------------------------------------------------------------------
		// Encrypt 
		//------------------------------------------------------------------------
		private bool FileEncrypt(string FilePath, string Password)
		{
			int i, len;
			byte[] buffer = new byte[4096];

			//Output file path.
			string OutFilePath = Path.Combine(Path.GetDirectoryName(FilePath), Path.GetFileNameWithoutExtension(FilePath)) + ".enc";

			using (FileStream outfs = new FileStream(OutFilePath, FileMode.Create, FileAccess.Write))
			{
				using (AesManaged aes = new AesManaged())
				{
					aes.BlockSize = 128;              // BlockSize = 16bytes
					aes.KeySize = 128;                // KeySize = 16bytes
					aes.Mode = CipherMode.CBC;        // CBC mode
					aes.Padding = PaddingMode.PKCS7;	// Padding mode is "PKCS7".
					// Password
					byte[] bufferKey = new byte[16];
					byte[] bufferPassword = Encoding.UTF8.GetBytes(Password);
					//パスワードは128bitなので、16バイトまで切り詰めるか、あるいはそのサイズまで埋める処理
					for (i = 0; i < bufferKey.Length; i++)
					{
						if (i < bufferPassword.Length)
						{
							//Cut down to 16bytes characters.
							bufferKey[i] = bufferPassword[i];
						}
						else
						{
							bufferKey[i] = 0;
						}
					}
					aes.Key = bufferKey;
					// Initilization Vector
					byte[] iv = new byte[16];
					RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
					rng.GetNonZeroBytes(iv);
					aes.IV = iv;

					//Encryption interface.
					ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

					using (CryptoStream cse = new CryptoStream(outfs, encryptor, CryptoStreamMode.Write))
					{	// IV
						outfs.Write(iv, 0, 16);	//ファイル先頭に埋め込む
						using (DeflateStream ds = new DeflateStream(cse, CompressionMode.Compress))	//圧縮
						{
							using (FileStream fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read))
							{
								while ((len = fs.Read(buffer, 0, 4096)) > 0)
								{
									ds.Write(buffer, 0, len);
								}
							}
						}

					}

				}
			}
			//Encryption succeed.
			textBox1.AppendText("暗号化成功: " + Path.GetFileName(OutFilePath) + Environment.NewLine);
			return (true);
		}

		//------------------------------------------------------------------------
		// Decrypt 
		//------------------------------------------------------------------------
		private bool FileDecrypt(string FilePath, string Password)
		{
			int i, len;
			byte[] buffer = new byte[4096];

			if (String.Compare(Path.GetExtension(FilePath), ".enc", true) != 0)
			{
				//The file are not encrypted file! Decryption failed
				MessageBox.Show("暗号化されたファイルではありません！" + Environment.NewLine + "復号に失敗しました。", 
					"Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return (false); ;
			}
			
			//Output file path.
			string OutFilePath = Path.Combine(Path.GetDirectoryName(FilePath), Path.GetFileNameWithoutExtension(FilePath)) + ".txt";

			using (FileStream outfs = new FileStream(OutFilePath, FileMode.Create, FileAccess.Write))
			{
				using (FileStream fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read))
				{
					using (AesManaged aes = new AesManaged())
					{
						aes.BlockSize = 128;              // BlockSize = 16bytes
						aes.KeySize = 128;                // KeySize = 16bytes
						aes.Mode = CipherMode.CBC;        // CBC mode
						aes.Padding = PaddingMode.PKCS7;	// Padding mode is "PKCS7".
						// Password
						byte[] bufferKey = new byte[16];
						byte[] bufferPassword = Encoding.UTF8.GetBytes(Password);
						//パスワードは128bitなので、16バイトまで切り詰めるか、あるいはそのサイズまで埋める処理
						for (i = 0; i < bufferKey.Length; i++)
						{
							if (i < bufferPassword.Length)
							{
								//Cut down to 16bytes characters.
								bufferKey[i] = bufferPassword[i];
							}
							else
							{
								bufferKey[i] = 0;
							}
						}
						aes.Key = bufferKey;
						// Initilization Vector
						byte[] iv = new byte[16];
						fs.Read(iv, 0, 16);	//ファイル先頭から取得する
						aes.IV = iv;

						//Decryption interface.
						ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

						using (CryptoStream cse = new CryptoStream(fs, decryptor, CryptoStreamMode.Read))
						{
							using (DeflateStream ds = new DeflateStream(cse, CompressionMode.Decompress))	//解凍
							{
								while ((len = ds.Read(buffer, 0, 4096)) > 0)
								{
									outfs.Write(buffer, 0, len);
								}
							}
						}
					}
				}
			}
			//Decryption succeed.
			textBox1.AppendText("復号成功: " + Path.GetFileName(OutFilePath) + Environment.NewLine);
			return (true);
		}
		
	}
}