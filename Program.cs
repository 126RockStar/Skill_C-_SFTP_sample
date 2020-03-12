using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using Renci.SshNet;


namespace sftp
{
    // メイン
    class Program
    {
        static void Main(string[] args)
        {
            // 
            var _Sftp = new CSftp();
            _Sftp.Execute();


            Console.WriteLine("press key..");
            Console.ReadKey();
        }
    }


    //======================================================================
    // SFTP接続クラス
    public class CSftp
    {
        // 接続情報
        public ConnectionInfo ConnNfo { private set; get; }
        // 接続ホスト名
        public string HostName { private set; get; }
        // ポート
        public Int32 Port { private set; get; }
        // ユーザー名
        public string UserName { private set; get; }
        // パスワード
        public string Password { private set; get; }


        //------------------------------------------------------------------
        // コンストラクタ
        public CSftp()
        {
            HostName = "aus-d4w-cloud.ozbackups.com.au";             // 接続先ホスト名
            Port = 22;                       // ポート
            UserName = "ozbd4wcloud";     // ユーザー名
            Password = "V2aCnU9>Xq+n6hFyXp4";                  // パスワード

            string KeyFile = @"E:\2.ppk";     // 秘密鍵
            string PassPhrase = "";                          // パスフレーズ

            // パスワード認証
            var _PassAuth = new PasswordAuthenticationMethod(UserName, Password);

            // 秘密鍵認証
            var _PrivateKey = new PrivateKeyAuthenticationMethod(UserName, new PrivateKeyFile[]{
                        new PrivateKeyFile(KeyFile, PassPhrase)
                    });
            //PrivateKeyFile privateKey;
            //using (var keystrm = new MemoryStream(Encoding.ASCII.GetBytes(keyStr)))
            //{
            //    privateKey = new PrivateKeyFile(keystrm);


            //}
            // 接続情報の生成
            //var _PrivateKey = new PrivateKeyAuthenticationMethod(UserName, privateKey);

            ConnNfo = new ConnectionInfo(HostName, Port, UserName,
                new AuthenticationMethod[]{
                        _PassAuth,          // パスワード認証
                        _PrivateKey,        // 秘密鍵認証
                }
            );

        }


        //------------------------------------------------------------------
        // 実行
        public void Execute()
        {

            using (var sftp = new SftpClient(ConnNfo))
            {
                // 接続
                sftp.Connect();
                // 確認
                if (sftp.IsConnected)
                {
                    // 接続に成功
                    Console.WriteLine("Connection success!!\n");
                }
                else
                {
                    // 接続に失敗
                    Console.WriteLine("Connection failed!!\n");
                    return;
                }


                // ファイルリスト表示
                //printFiles(sftp, "/remote");

                // ファイル内容表示
                //printTxtFile(sftp, "/remote/test.txt");

                // ファイルアップロード
                uploadFile(sftp, "/ozbackups_d4w", "../../Program.cs");


                // 切断
                sftp.Disconnect();
            }

        }


        //------------------------------------------------------------------
        // ファイル表示
        private void printFiles(
            SftpClient _sftp,   // sftpクライアント
            string _Path        // パス
            )
        {
            // ★日本語ファイル名を扱う際の注意★
            // ファイルパスなどはConnectionInfoクラスの中にEncodingというプロパティにエンコーディング情報を設定する。
            // SftpClientのread/write等でのファイル内容でのエンコーディングは別途引数で指定する。

            // 指定パスを調べる
            foreach (var file in _sftp.ListDirectory(_Path))
            {
                if (file.Name.StartsWith(".")) continue;

                if (file.IsDirectory)
                {
                    // ディレクトリなら再帰して調べる
                    printFiles(_sftp, file.FullName);
                }
                else
                {
                    // 表示
                    Console.WriteLine($"{file.FullName}\t\t{file.LastAccessTime}\t{file.LastWriteTime}");
                }
            }

        }


        //------------------------------------------------------------------
        // 指定テキストファイルの表示
        private void printTxtFile(
            SftpClient _sftp,       // sftpクライアント
            string _FilePath        // ファイルパス
            )
        {
            var _CurDir = Path.GetDirectoryName(_FilePath).Substring(1);
            var _FileName = Path.GetFileName(_FilePath);

            // カレントディレクトリ変更
            _sftp.ChangeDirectory(_CurDir);
            
            foreach (var file in _sftp.ListDirectory("./"))
            {
                if (file.IsDirectory) continue;
                if (file.Name != _FileName) continue;

                // 読み込み
                Int64 _Size = file.Length;
                var _Buf = new byte[_Size];
                using (var _St = new MemoryStream(_Buf, 0, (int)_Size))
                {
                    _sftp.DownloadFile(file.FullName, _St);
                }

                // SJIS変換
                string _str = Encoding.GetEncoding(932).GetString(_Buf);
                // 内容表示
                Console.WriteLine();
                Console.WriteLine($"------------------{file.Name}");
                Console.WriteLine($"{_str}");
                Console.WriteLine("------------------");
            }

        }


        //-------------------------------------------------------------------
        // ファイルのアップロード
        private void uploadFile(
            SftpClient _sftp,       // sftpクライアント
            string  _UploadPath,    // アップロードパス
            string  _UploadFile     // アップロードファイル名
            )
        {
            // カレントディレクトリ変更
            _sftp.ChangeDirectory(_UploadPath);
            // アップロード先パス
            var _RemotePath = _UploadPath + "/" + Path.GetFileName(_UploadFile);

            using (var _uploadStream = File.OpenRead(_UploadFile))
            {
                _sftp.UploadFile(_uploadStream, _RemotePath, true);
            }


        }
        
    }


}
