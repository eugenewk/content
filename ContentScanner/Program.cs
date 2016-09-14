using System;

namespace ContentScanner
{
    class Program
    {
        static void Main()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("=======================");
            Console.WriteLine(" Content Scanner v0.10 ");
            Console.WriteLine("=======================");
            Console.ForegroundColor = ConsoleColor.White;

            Help();

            while (true)
            {
                Console.Write("\ncontent-scanner>");
                string command = Console.ReadLine();

                if (command == "scan")
                {
                    new Scanner();
                    // the constructor for this class executes the scan
                    // see Scanner.cs for class definition
                }
                else if (command == "map")
                {
                    new Mapper();
                    // the constructor for this class executes the scan
                    // see Mapper.cs for class definition
                }
                else if (command == "help")
                {
                    Help();
                }
                else if (command == "quit")
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Please enter a valid command.");
                }
            }
        }

        static void Help()
        {
            Console.WriteLine("\nAvailable commands:\n");
            Console.WriteLine("'scan' outputs list of all subdirectories and files");
            Console.WriteLine("'map' outputs indented folder taxonomy");
            Console.WriteLine("'help' brings up this menu");
            Console.WriteLine("'quit'...quits");
        }      
    }
}
