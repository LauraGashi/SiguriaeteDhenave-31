using MySql.Data.MySqlClient;
using Server.JWT.Managers;
using Server.JWT.Models;
using Server.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;


namespace Server
{
    class Program
    {

        public static DESCryptoServiceProvider des = new DESCryptoServiceProvider();

        private static byte[] desKey;
        private static byte[] desIv;


        static void Main(string[] args)
        {
            string[] signUp;
            string[] logIn = null;
            int recv;
            byte[] data = new byte[1024];
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, 12000);

            Socket newSocket = new Socket(AddressFamily.InterNetwork,
                                        SocketType.Dgram, ProtocolType.Udp);    //Ruajtja e connection qe e marrim
            newSocket.Bind(endpoint);   //lidhja e cdo connection ne mberritje

            Console.WriteLine("Duke pritur per nje klient.....");

            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 12000);   //Lidhje e cdo pajisjeje(klienti) me qfardo IP dhe porti: 12000
            EndPoint tempRemote = (EndPoint)sender;     //variabla qe e ruan klinetin
            Kthehu:
            while (true)
            {

                data = new byte[1024];      //resetimi i byte[]
                recv = newSocket.ReceiveFrom(data, ref tempRemote);
                Console.WriteLine(Encoding.ASCII.GetString(data, 0, recv));     //nese ka te dhena per tu lexuar, atehere i shfaqim ato 

                string[] result = Encoding.ASCII.GetString(data, 0, recv).Split(' ');

                // ***....
                // ***....
                for (int i = 0; i < data.Length; i++)
                {
                    Console.WriteLine(data[i] + " Length= " + result[i].Length);

                }
                Console.WriteLine(result.Length);


                int messageLength = result[2].Length;

                byte[] message = new byte[messageLength];

                int length = result[1].Length;
                Console.WriteLine(length);
                desKey = new byte[length];
                
                desKey = DecryptDataOaepSha1(cert, Convert.FromBase64String(result[1]));
                int ivlength = result[0].Length;

                desIv = new byte[ivlength];

                desIv = Convert.FromBase64String(result[0]);
                Console.WriteLine("Gjatesia e pranuar" + data.Length);
                Console.WriteLine(Convert.ToBase64String(desKey));

                byte[] decryptedMessage = DekriptoDes(result[2]);

                Console.WriteLine(Convert.ToBase64String(decryptedMessage));

                string[] tedhenat = Encoding.UTF8.GetString(decryptedMessage).Split(':');

                if (result.Length > 2)
                {

                    signUp = result;

                    //**********************************

                    string connectionString = @"server=localhost;userid=root;password=;database=siguri";

                    MySqlConnection connection = null;
                    try
                    {
                        byte[] bytePlainText = System.Text.Encoding.UTF8.GetBytes(signUp[4]); ;
                        byte[] byteSalt = CreateSalt();
                        string salt = System.Convert.ToBase64String(byteSalt);
                        String hashedSaltedPass = GenerateSaltedHash(bytePlainText, byteSalt);

                        connection = new MySqlConnection(connectionString);
                        connection.Open();
                        MySqlCommand cmd = new MySqlCommand();
                        cmd.Connection = connection;
                        cmd.CommandText = " INSERT INTO `projekti`(`flname`, `email`, `password`, `confirmpassword`, `fatura`, `vitimuaji`, `vleraeuro`, `salt`) VALUES ('@fl','@em','@pw','@cp','@fa','@vm','@ve','@sa')";
                        
                        cmd.Prepare();

                        cmd.Parameters.AddWithValue("@fl", signUp[0]);
                        cmd.Parameters.AddWithValue("@em", signUp[1]);
                        cmd.Parameters.AddWithValue("@pw", hashedSaltedPass);
                        cmd.Parameters.AddWithValue("@cp", hashedSaltedPass);
                        cmd.Parameters.AddWithValue("@fa", signUp[4]);
                        cmd.Parameters.AddWithValue("@vm", signUp[5]);
                        cmd.Parameters.AddWithValue("@ve", signUp[6]);
                        cmd.Parameters.AddWithValue("@salt", salt);
                        

                        // check if the textboxes contains the default values 
                        if (!checkTextBoxesValues())
                        {
                            // check if the password equal the confirm password
                            if (signUp[2].Equals(signUp[3]))
                            {
                                // check if this username already exists
                                if (checkUsername())
                                {
           
                                    Console.WriteLine("This Username Already Exists, Select A Different One", "Duplicate Username");
                                }
                                else
                                {
                                    // execute the query
                                    if (cmd.ExecuteNonQuery() == 1)
                                    {
                                    Console.WriteLine("Your Account Has Been Created", "Account Created");
                                    }
                                    else
                                    {
                                    Console.WriteLine("ERROR");
                                    }
                                }
                            }
                            else
                            {
                            Console.WriteLine("Wrong Confirmation Password", "Password Error");
                            }

                        }
                        else
                        {
                        Console.WriteLine("Enter Your Informations First", "Empty Data");
                        }


                        }
                        finally
                        {
                           if (connection != null)
                             connection.Close();
                        }

                    // *-*-/- 
                    IAuthContainerModel model = GetJWTContainerModel(signUp[5], signUp[2]);
                    IAuthService authService = new JWTService(model.SecretKey);

                    string token = authService.GenerateToken(model);

                    if (!authService.IsTokenValid(token))
                        throw new UnauthorizedAccessException();
                    else
                    {
                        List<Claim> claims = authService.GetTokenClaims(token).ToList();

                        Console.WriteLine(claims.FirstOrDefault(e => e.Type.Equals(ClaimTypes.Name)).Value);
                        Console.WriteLine(claims.FirstOrDefault(e => e.Type.Equals(ClaimTypes.Email)).Value);
                    }
                    // *-*-/- 


                    // check if the username already exists
                    Boolean checkUsername()
                    {
                        DB db = new DB();

                        String fatura = signUp[5];

                        DataTable table = new DataTable();

                        MySqlDataAdapter adapter = new MySqlDataAdapter();

                        MySqlCommand command = new MySqlCommand("SELECT * FROM `projekti` WHERE `fatura = @fa", db.getConnection());

                        command.Parameters.Add("@fa", MySqlDbType.VarChar).Value = username;

                        adapter.SelectCommand = command;

                        adapter.Fill(table);

                        // check if this username already exists in the database
                        if (table.Rows.Count > 0)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }

                    }

                    // check if the textboxes contains the default values
                    Boolean checkTextBoxesValues()
                    {
                        String fl = signUp[0];
                        String em = signUp[1];
                        String pw = signUp[2];
                        String cp = signUp[3];
                        String fa = signUp[4];
                        String vm = signUp[5];
                        String ve = signUp[6];

                        

                        if (flname.Equals("firstname and last name") || email.Equals("email") ||
                           password.Equals("password") || confirmpassword.Equals("confirmpassword")
                            || fatura.Equals("fatura") || vitimuaji.Equals("vitimuaji") || vleraeuro("vleraeuro"))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }


                }

                else {
                    logIn = result;

                    string connectionString = @"server=localhost;userid=root;password=;database=siguri";

                    MySqlConnection connection = null;
                    MySqlDataReader reader = null;
                    try
                    {
                        connection = new MySqlConnection(connectionString);
                        connection.Open();


                        string stm = "SELECT * FROM `users` WHERE `username` = '" + logIn[0] + "'"; //and `password` = '" +logIn[1]+"'";
                        MySqlDataAdapter dataAdapter = new MySqlDataAdapter();
                        dataAdapter.SelectCommand = new MySqlCommand(stm, connection);
                        DataTable table = new DataTable();
                        dataAdapter.Fill(table);
                        if (table.Rows.Count > 0)
                        {
                            Console.WriteLine("Username found");
                            string salt = table.Rows[0]["salt"].ToString();
                            string pass = table.Rows[0]["password"].ToString();
                            string id = table.Rows[0]["id"].ToString();
                            byte[] byteSalt = System.Convert.FromBase64String(salt);
                            byte[] bytePlainText = System.Text.Encoding.UTF8.GetBytes(logIn[1]);
                            string hashedSaltedPass = GenerateSaltedHash(bytePlainText, byteSalt);
                            if (pass.Equals(hashedSaltedPass))
                            {
                                Console.WriteLine("Loged in");
                                string query = "SELECT * FROM `grades` WHERE `userid` =' " + id + "'";
                                dataAdapter = new MySqlDataAdapter();
                                dataAdapter.SelectCommand = new MySqlCommand(query, connection);
                                DataTable table1 = new DataTable();
                                dataAdapter.Fill(table1);
                                string test = null;
                                for (int i = 0; table1.Rows.Count > i; i++)
                                {
                                    test += table1.Rows[i]["fatura"].ToString() + " " + table1.Rows[i]["vlera"].ToString() + "\n";
                                }

                                byte[] packetData = System.Text.ASCIIEncoding.ASCII.GetBytes(test);
                                newSocket.SendTo(packetData, tempRemote);

                            }
                            else
                            {
                                Console.WriteLine("Wrong password/username");
                                byte[] packetData = System.Text.ASCIIEncoding.ASCII.GetBytes("Wrong password/username");
                                newSocket.SendTo(packetData, tempRemote);
                                goto Kthehu;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Wrong password/username");
                            byte[] packetData = System.Text.ASCIIEncoding.ASCII.GetBytes("Wrong password/username");
                            newSocket.SendTo(packetData, tempRemote);
                            goto Kthehu;
                        }
                    }
                    finally
                    {
                        if (reader != null)
                            reader.Close();
                        if (connection != null)
                            connection.Close();
                    }



                }

            }
     

        private static JWTContainerModel GetJWTContainerModel(string name, string email)
        {
            return new JWTContainerModel()
            {
                Claims = new Claim[]
                {
                    new Claim(ClaimTypes.Name, name),
                    new Claim(ClaimTypes.Email, email)
                }
            };
        }

       

        private static byte[] CreateSalt()
        {
            //Generate a cryptographic random number.
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] buff = new byte[16];
            rng.GetBytes(buff);

            // Return a Base64 string representation of the random number.
            return buff;
        }
        private static string GenerateSaltedHash(byte[] plainText, byte[] salt)
        {
            HashAlgorithm algorithm = new SHA256Managed();

            byte[] plainTextWithSaltBytes =
              new byte[plainText.Length + salt.Length];

            for (int i = 0; i < plainText.Length; i++)
            {
                plainTextWithSaltBytes[i] = plainText[i];
            }
            for (int i = 0; i < salt.Length; i++)
            {
                plainTextWithSaltBytes[plainText.Length + i] = salt[i];
            }
            byte[] hash = algorithm.ComputeHash(plainTextWithSaltBytes);
            return System.Convert.ToBase64String(hash); ;

        }

        public static byte[] DecryptDataOaepSha1(X509Certificate2 cert, byte[] data)
        {
            using (RSA rsa = cert.GetRSAPrivateKey())
            {
                return rsa.Decrypt(data, RSAEncryptionPadding.OaepSHA1);
            }
        }
        private static X509Certificate2 GetCertificateFromStore(string certName)
        {

            // Get the certificate store for the current user.
            X509Store store = new X509Store(StoreLocation.CurrentUser);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection certCollection = store.Certificates;
                X509Certificate2Collection currentCerts = certCollection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);
                X509Certificate2Collection signingCert = currentCerts.Find(X509FindType.FindBySubjectDistinguishedName, certName, false);
                if (signingCert.Count == 0)
                    return null;
                return signingCert[0];
            }
            finally
            {
                store.Close();
            }

        }


        public static byte[] Enkripto(String plaintext)
        {
            des.Padding = PaddingMode.Zeros;
            des.Key = desKey;
            des.IV = desIv;


            byte[] bytePlaintexti = Encoding.UTF8.GetBytes(plaintext);

            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms,
                                des.CreateEncryptor(),CryptoStreamMode.Write);
            cs.Write(bytePlaintexti, 0, bytePlaintexti.Length);
            cs.Close();

            byte[] byteCiphertexti = ms.ToArray();

            return byteCiphertexti;
        }


        public static byte[] DekriptoDes(string ciphertext)
        {
            des.Padding = PaddingMode.Zeros;
            des.Mode = CipherMode.CBC;
            des.Key = desKey;
            des.IV = desIv;


            byte[] byteCiphertexti =
                Convert.FromBase64String(ciphertext);
            MemoryStream ms = new MemoryStream(byteCiphertexti);
            CryptoStream cs =
                new CryptoStream(ms,
                des.CreateDecryptor(),
                CryptoStreamMode.Read);

            byte[] byteTextiDekriptuar = new byte[ms.Length];
            cs.Read(byteTextiDekriptuar, 0, byteTextiDekriptuar.Length);
            cs.Close();

            return byteTextiDekriptuar;
        }
    }

}

        

