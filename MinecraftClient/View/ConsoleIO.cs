﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace MinecraftClient
{
    /// <summary>
    /// Allows simultaneous console input and output without breaking user input
    /// (Without having this annoying behaviour : User inp[Some Console output]ut)
    /// Provide some fancy features such as formatted output, text pasting and tab-completion.
    /// By ORelio - (c) 2012-2018 - Available under the CDDL-1.0 license
    /// </summary>
    public static class ConsoleIO
    {
        private static IAutoComplete autocomplete_engine;
        private static LinkedList<string> autocomplete_words = new LinkedList<string>();
        private static LinkedList<string> previous = new LinkedList<string>();
        private static readonly object io_lock = new object();
        private static bool reading = false;
        private static string buffer = "";
        private static string buffer2 = "";

        /// <summary>
        /// Reset the IO mechanism and clear all buffers
        /// </summary>
        public static void Reset()
        {
            lock (io_lock)
            {
                if (reading)
                {
                    ClearLineAndBuffer();
                    reading = false;
                    Console.Write("\b \b");
                }
            }
        }

        public static void SetAutoCompleteEngine(IAutoComplete engine)
        {
            autocomplete_engine = engine;
        }

        /// <summary>
        /// Determine whether WriteLineFormatted() should prepend lines with timestamps by default.
        /// </summary>
        public static bool EnableTimestamps = false;

        public static string LogPrefix = "§8[Log] ";

        public static string ReadPassword()
        {
            StringBuilder password = new StringBuilder();

            ConsoleKeyInfo k;
            while ((k = Console.ReadKey(true)).Key != ConsoleKey.Enter)
            {
                switch (k.Key)
                {
                    case ConsoleKey.Backspace:
                        if (password.Length > 0)
                        {
                            Console.Write("\b \b");
                            password.Remove(password.Length - 1, 1);
                        }
                        break;

                    case ConsoleKey.Escape:
                    case ConsoleKey.LeftArrow:
                    case ConsoleKey.RightArrow:
                    case ConsoleKey.Home:
                    case ConsoleKey.End:
                    case ConsoleKey.Delete:
                    case ConsoleKey.DownArrow:
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.Tab:
                        break;

                    default:
                        if (k.KeyChar != 0)
                        {
                            Console.Write('*');
                            password.Append(k.KeyChar);
                        }
                        break;
                }
            }

            Console.WriteLine();
            return password.ToString();
        }

        public static string ReadLine()
        {
           return Console.ReadLine();
        }

        /// <summary>
        /// Debug routine: print all keys pressed in the console
        /// </summary>
        public static void DebugReadInput()
        {
            ConsoleKeyInfo k = new ConsoleKeyInfo();
            while (true)
            {
                k = Console.ReadKey(true);
                Console.WriteLine("Key: {0}\tChar: {1}\tModifiers: {2}", k.Key, k.KeyChar, k.Modifiers);
            }
        }

        public static void Write(string text)
        {
            Console.Write(text);
        }

        public static void WriteLine(string line)
        {
            Write(line + '\n');
        }

        public static void Write(char c)
        {
            Write("" + c);
        }

        /// <summary>
        /// Write a Minecraft-Like formatted string to the standard output, using §c color codes
        /// See minecraft.gamepedia.com/Classic_server_protocol#Color_Codes for more info
        /// </summary>
        /// <param name="acceptnewlines">If false, space are printed instead of newlines</param>
        /// If false, no timestamp is prepended.
        /// If true, "hh-mm-ss" timestamp will be prepended.
        /// If unspecified, value is retrieved from EnableTimestamps.
        /// </param>
        public static void WriteLineFormatted(string str, bool acceptnewlines = true, bool? displayTimestamp = null)
        {
            if (!String.IsNullOrEmpty(str))
            {
                if (!acceptnewlines)
                {
                    str = str.Replace('\n', ' ');
                }
                if (displayTimestamp == null)
                {
                    displayTimestamp = EnableTimestamps;
                }
                if (displayTimestamp.Value)
                {
                    int hour = DateTime.Now.Hour, minute = DateTime.Now.Minute, second = DateTime.Now.Second;
                    ConsoleIO.Write(String.Format("{0}:{1}:{2} ", hour.ToString("00"), minute.ToString("00"), second.ToString("00")));
                }

                //Console.WriteLine(str);
                //return;

                string[] parts = str.Split(new char[] { '§' });
                if (parts[0].Length > 0)
                {
                    ConsoleIO.Write(parts[0]);
                }
                for (int i = 1; i < parts.Length; i++)
                {
                    if (parts[i].Length > 0)
                    {
                        switch (parts[i][0])
                        {
                            case '0': Console.ForegroundColor = ConsoleColor.Gray; break; //Should be Black but Black is non-readable on a black background
                            case '1': Console.ForegroundColor = ConsoleColor.DarkBlue; break;
                            case '2': Console.ForegroundColor = ConsoleColor.DarkGreen; break;
                            case '3': Console.ForegroundColor = ConsoleColor.DarkCyan; break;
                            case '4': Console.ForegroundColor = ConsoleColor.DarkRed; break;
                            case '5': Console.ForegroundColor = ConsoleColor.DarkMagenta; break;
                            case '6': Console.ForegroundColor = ConsoleColor.DarkYellow; break;
                            case '7': Console.ForegroundColor = ConsoleColor.Gray; break;
                            case '8': Console.ForegroundColor = ConsoleColor.DarkGray; break;
                            case '9': Console.ForegroundColor = ConsoleColor.Blue; break;
                            case 'a': Console.ForegroundColor = ConsoleColor.Green; break;
                            case 'b': Console.ForegroundColor = ConsoleColor.Cyan; break;
                            case 'c': Console.ForegroundColor = ConsoleColor.Red; break;
                            case 'd': Console.ForegroundColor = ConsoleColor.Magenta; break;
                            case 'e': Console.ForegroundColor = ConsoleColor.Yellow; break;
                            case 'f': Console.ForegroundColor = ConsoleColor.White; break;
                            case 'r': Console.ForegroundColor = ConsoleColor.Gray; break;
                        }

                        if (parts[i].Length > 1)
                        {
                            ConsoleIO.Write(parts[i].Substring(1, parts[i].Length - 1));
                        }
                    }
                }
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            ConsoleIO.Write('\n');
        }

        public static void WriteLogLine(string text)
        {
            WriteLineFormatted(LogPrefix + text);
        }

        #region Subfunctions

        /// <summary>
        /// Clear all text inside the input prompt
        /// </summary>
        private static void ClearLineAndBuffer()
        {
            while (buffer2.Length > 0)
            {
                GoRight();
            }
            while (buffer.Length > 0)
            {
                RemoveOneChar();
            }
        }

        /// <summary>
        /// Remove one character on the left of the cursor in input prompt
        /// </summary>
        private static void RemoveOneChar()
        {
            if (buffer.Length > 0)
            {
                try
                {
                    if (Console.CursorLeft == 0)
                    {
                        Console.CursorLeft = Console.BufferWidth - 1;
                        if (Console.CursorTop > 0)
                            Console.CursorTop--;
                        Console.Write(' ');
                        Console.CursorLeft = Console.BufferWidth - 1;
                        if (Console.CursorTop > 0)
                            Console.CursorTop--;
                    }
                    else Console.Write("\b \b");
                }
                catch (ArgumentOutOfRangeException) { /* Console was resized!? */ }
                buffer = buffer.Substring(0, buffer.Length - 1);

                if (buffer2.Length > 0)
                {
                    Console.Write(buffer2 + " \b");
                    for (int i = 0; i < buffer2.Length; i++)
                    {
                        GoBack();
                    }
                }
            }
        }

        /// <summary>
        /// Move the cursor one character to the left inside the console, regardless of input prompt state
        /// </summary>
        private static void GoBack()
        {
            try
            {
                if (Console.CursorLeft == 0)
                {
                    Console.CursorLeft = Console.BufferWidth - 1;
                    if (Console.CursorTop > 0)
                        Console.CursorTop--;
                }
                else Console.Write('\b');
            }
            catch (ArgumentOutOfRangeException) { /* Console was resized!? */ }
        }

        /// <summary>
        /// Move the cursor one character to the left in input prompt, adjusting buffers accordingly
        /// </summary>
        private static void GoLeft()
        {
            if (buffer.Length > 0)
            {
                buffer2 = "" + buffer[buffer.Length - 1] + buffer2;
                buffer = buffer.Substring(0, buffer.Length - 1);
                Console.Write('\b');
            }
        }

        /// <summary>
        /// Move the cursor one character to the right in input prompt, adjusting buffers accordingly
        /// </summary>
        private static void GoRight()
        {
            if (buffer2.Length > 0)
            {
                buffer = buffer + buffer2[0];
                Console.Write(buffer2[0]);
                buffer2 = buffer2.Substring(1);
            }
        }

        /// <summary>
        /// Insert a new character in the input prompt
        /// </summary>
        /// <param name="c">New character</param>
        private static void AddChar(char c)
        {
            Console.Write(c);
            buffer += c;
            Console.Write(buffer2);
            for (int i = 0; i < buffer2.Length; i++)
            {
                GoBack();
            }
        }

        #endregion

        #region Clipboard management

        /// <summary>
        /// Read a string from the Windows clipboard
        /// </summary>
        /// <returns>String from the Windows clipboard</returns>
        private static string ReadClipboard()
        {
            string clipdata = "";
            Thread staThread = new Thread(new ThreadStart(
                delegate
                {
                    try
                    {
                        clipdata = Clipboard.GetText();
                    }
                    catch { }
                }
            ));
            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
            staThread.Join();
            return clipdata;
        }

        #endregion
    }


    public interface IAutoComplete
    {
        /// <summary>
        /// Provide a list of auto-complete strings based on the provided input behing the cursor
        /// </summary>
        /// <param name="BehindCursor">Text behind the cursor, e.g. "my input comm"</param>
        /// <returns>List of auto-complete words, e.g. ["command", "comment"]</returns>
        IEnumerable<string> AutoComplete(string BehindCursor);
    }
}
