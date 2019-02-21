using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSAN_Lab1_cmd
{
    class Program
    {
        public class MXNetworkManager
        {
            public List<MXNetworkAdapter> activeAdapters;

            public MXNetworkManager()
            {
                activeAdapters = GetParsedIpConfig();
            }

            private List<MXNetworkAdapter> GetParsedIpConfig()
            {
                List<MXNetworkAdapter> result = new List<MXNetworkAdapter>();
                using (System.IO.StringReader reader = new System.IO.StringReader(Cmd("ipconfig")))
                {
                    string buffer = "";
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Contains("adapter"))
                        {
                            if (buffer.Contains("Default Gateway"))
                            {
                                result.Add(new MXNetworkAdapter(buffer));
                            }
                            buffer = "";
                        }
                        buffer += line + '\n';
                    }

                    if (buffer.Contains("Default Gateway"))
                    {
                        //string temp = buffer.Remove(0, buffer.IndexOf("Default Gateway"));
                        if (buffer.Remove(0, buffer.IndexOf(':')).Length > 5)
                        {
                            result.Add(new MXNetworkAdapter(buffer));
                        }
                    }
                }
                return result;
            }
        }

        public class MXNetworkAdapter
        {
            public string name;
            public MXNetAddress ipv4_Addr;
            public MXNetAddress subnetMask;



            public MXNetworkAdapter(string name, string ip, string mask, string defGateway)
            {
                this.name = name;
                ipv4_Addr = new MXNetAddress(ip);
                subnetMask = new MXNetAddress(mask);
            }

            public MXNetworkAdapter(string rawIpConfig)
            {
                using (System.IO.StringReader reader = new System.IO.StringReader(rawIpConfig))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Contains("adapter"))
                        {
                            this.name = line.Replace(":", "");
                        }
                        else if (line.Contains("IPv4 Address"))
                        {
                            ipv4_Addr = new MXNetAddress(line.Remove(0, line.IndexOf(':') + 2));
                        }
                        else if (line.Contains("Subnet Mask"))
                        {
                            subnetMask = new MXNetAddress(line.Remove(0, line.IndexOf(':') + 2));
                        }
                    }
                }
            }

            public List<string> GetDevices()
            {
                List<string> rawResult = new List<string>();
                string arpResult = "";
                for (var ip = new MXNetAddress(ipv4_Addr, subnetMask); ip.value < (ipv4_Addr.value | ~subnetMask.value); ip.inc())
                {
                    arpResult = Cmd("arp -a " + ip.address);
                    var temp = arpResult.Split('\n');
                    if (temp.Length < 3) {
                        rawResult.Add(temp[0]);
                    }
                    else
                    {
                        rawResult.Add(temp[3]);
                    }
                }

                List<string> result = new List<string>();

                foreach (var item in rawResult)
                {
                    if (!item.Contains("Entries"))
                    {
                        result.Add(item);
                    }
                }

                if (result.Count == 0) result.Add("No ARP Entries Found");
                
                return result;
            }
        }

        public class MXNetAddress
        {
            public byte[] bytes = new byte[4];
            public uint value = 0;
            public string address = "";

            public MXNetAddress(MXNetAddress ip, MXNetAddress mask)
            {
                for (int i = 0; i < 4; i++) bytes[i] = (byte)(ip.bytes[i] & mask.bytes[i]);
                value = ip.value & mask.value;
                address = bytes[0].ToString() + '.' +
                          bytes[1].ToString() + '.' +
                          bytes[2].ToString() + '.' +
                          bytes[3].ToString();
            }

            public MXNetAddress(string address)
            {
                value = 0;
                this.address = address;
                string[] temp = address.Split('.');
                for (var i = 0; i < 4; i++)
                {
                    bytes[i] = Convert.ToByte(temp[i]);
                    value += (uint)(bytes[i] << (8 * (3 - i)));
                }
            }

            public void inc()
            {
                value++;
                if (bytes[3] == 255)
                {
                    bytes[3] = 0;
                    if (bytes[2] == 255)
                    {
                        bytes[2] = 0;
                        if (bytes[1] == 255)
                        {
                            bytes[1] = 0;
                            if (bytes[0] == 255)
                            {
                                for (var i = 0; i < 4; i++) bytes[i] = 0;
                            } else bytes[0]++;
                        } else bytes[1]++;
                    } else bytes[2]++;
                } else bytes[3]++;

                address = bytes[0].ToString() + '.' +
                          bytes[1].ToString() + '.' +
                          bytes[2].ToString() + '.' +
                          bytes[3].ToString();
            }

            public string GetMac()
            {
                string arp = Cmd("arp -a");
                using (System.IO.StringReader reader = new System.IO.StringReader(arp))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Contains(address) && !line.Contains("Interface"))
                        {
                            return line.Substring(23, 17);
                        }
                    }
                }
                return "";
            }
        }

        static void Main()
        {
            var networkManager = new MXNetworkManager();

            foreach (var adapter in networkManager.activeAdapters)
            {
                print(adapter.name);
                print("ipv4_address : " + adapter.ipv4_Addr.address);
                print("subnet_mask  : " + adapter.subnetMask.address);
                foreach (var device in adapter.GetDevices())
                {
                    print(device);
                }
                print();
            }

            print("Press enter to exit");
            Console.ReadLine();
        }

        public static string Cmd(string args, System.Diagnostics.Process process = null)
        {
            System.Diagnostics.Process cmd;
            if (process == null) cmd = new System.Diagnostics.Process();
            else cmd = process;
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.Arguments = "/C" + args;
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            var result = cmd.StandardOutput.ReadToEnd();
            cmd.Close();
            return result;

        }
        #region prints
        public static void print(string text = "")
        {
            Console.WriteLine(text);
        }
        public static void print(string[] text)
        {
            Console.WriteLine(text);
        }
        #endregion
    }
}
