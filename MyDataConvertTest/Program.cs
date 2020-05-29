using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDataConvertTest
{
    class Program
    {
        static void Main(string[] args)
        {
            String Json = File.ReadAllText(System.IO.Directory.GetCurrentDirectory() + "\\json.txt");
            DataSet ds = DataConvert.DataJson.jsonToDataSet(Json);

            Console.ReadKey();
        }
    }
}
