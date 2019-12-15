using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
//using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System.Text;
using System.Threading;

namespace LatexConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            int browserWarmUp = 500; //in MS
            int browserRender = 1500; //in MS

            string file = "";
            if (args.Length == 0)
            {
                Console.WriteLine("Enter full path of Markdown file:");
                file = getValidFile();
            }
            else
            {
                file = args[0];
                if (!System.IO.File.Exists(file))
                {
                    Console.WriteLine("That file does not exist!");
                    return;
                }
            }

            //string file = @"C:\Users\maxim\Desktop\js\sqs\readme\ReadmeMD.NJ\readme.md";
            string fileName = System.IO.Path.GetFileNameWithoutExtension(file);
            string resFile = System.IO.Path.GetDirectoryName(file)+"\\"+System.IO.Path.GetFileNameWithoutExtension(file)+".noJax.md"; //@"C:\Users\maxim\Desktop\js\sqs\readme\ReadmeMD.NJ\readme.noJax.md";
            string imgDir = System.IO.Path.GetDirectoryName(file) + "\\"; //@"C:\Users\maxim\Desktop\js\sqs\readme\ReadmeMD.NJ\";

            string fileContents = System.IO.File.ReadAllText(file);

            Regex MultiLine = new Regex(@"(?<!\\)\$\$(?s)(.*?)(?<!\\)\$\$"); //MultiLine Regex
            Regex SingleLine = new Regex(@"(?<!\\)\$(?s)(.*?)(?<!\\)\$"); //Inline Regex, remove 0 length matches.

            List<Tuple<int, int, string>> mathPointers = new List<Tuple<int, int, string>>();

            Dictionary<string, string> RenderStrings = new Dictionary<string, string>();

            MatchCollection SingleLineRenders = SingleLine.Matches(fileContents);
            foreach (Match match in SingleLineRenders)
            {
                if (match.Value.Length <= 2) continue;
                //Console.WriteLine("{0}, {1}", match.Value.Substring(1, match.Value.Length - 2), match.Length);
                //string before = fileContents.Substring(cIndex, match.Index - cIndex);
                //peicesWithoutMath.Add(before);
                string Latex = match.Value.Substring(1, match.Value.Length - 2);
                mathPointers.Add(new Tuple<int, int, string>(match.Index, 0, Latex));
                if (!RenderStrings.ContainsKey(Latex)) RenderStrings.Add(Latex, "");
            }

            MatchCollection MultiLineRenders = MultiLine.Matches(fileContents);
            foreach (Match match in MultiLineRenders)
            {
                //Console.WriteLine("{0}, {1}", match.Value, match.Length);
                //string before = fileContents.Substring(cIndex, match.Index - cIndex);
                //peicesWithoutMath.Add(before);
                string Latex = match.Value.Substring(2, match.Value.Length - 4);
                mathPointers.Add(new Tuple<int, int, string>(match.Index, 1, Latex));
                if (!RenderStrings.ContainsKey(Latex)) RenderStrings.Add(Latex, "");
            }

            mathPointers = mathPointers.OrderBy((x)=>x.Item1).ToList();

            List<string> peicesWithoutMath = new List<string>();
            int cIndex = 0;

            for (int i = 0; i < mathPointers.Count; i++)
            {
                //Console.WriteLine(mathPointers[i].Item1);
                if (mathPointers[i].Item2 == 0)
                {
                    //Console.WriteLine("===");
                    string before = fileContents.Substring(cIndex, mathPointers[i].Item1 - cIndex);
                    //Console.WriteLine(before);
                    cIndex = mathPointers[i].Item1 + mathPointers[i].Item3.Length + 2;
                    peicesWithoutMath.Add(before);
                    //Console.WriteLine("===");
                }
                else
                {
                    //Console.WriteLine("===");
                    string before = fileContents.Substring(cIndex, mathPointers[i].Item1 - cIndex);
                    Console.WriteLine(before);
                    cIndex = mathPointers[i].Item1 + mathPointers[i].Item3.Length + 4;
                    peicesWithoutMath.Add(before);
                    //Console.WriteLine("===");
                }
            }

            string end = fileContents.Substring(cIndex, fileContents.Length - cIndex);//+ 1);
            //Console.WriteLine("END");
            //Console.WriteLine("===");
            //Console.WriteLine(end);
            //Console.WriteLine("===");
            peicesWithoutMath.Add(end);

            //new DriverManager().SetUpDriver(new ChromeConfig());
            IWebDriver driver = new ChromeDriver();

            //driver.Manage().Window.Minimize();
            driver.Navigate().GoToUrl("about:blank");

            //JS min
            string jsRender = "window.MathJax = { jax: [\"input/TeX\", \"output/SVG\"], extensions: [\"tex2jax.js\", \"MathMenu.js\", \"MathZoom.js\"], showMathMenu: false, showProcessingMessages: false, messageStyle: \"none\",SVG: {useGlobalCache: false}, TeX:{ extensions: [\"AMSmath.js\", \"AMSsymbols.js\", \"autoload-all.js\"]},};(function(d, script) { script = d.createElement('script'); script.type = 'text/javascript'; script.async = true; script.onload = function(){};script.src = 'https://cdnjs.cloudflare.com/ajax/libs/mathjax/2.7.0/MathJax.js';d.getElementsByTagName('head')[0].appendChild(script);}(document)); function mj2img(texstring, callback){ var input = texstring; var wrapper = document.createElement(\"div\");wrapper.innerHTML = input; var output = { svg: \"\", img: \"\"}; MathJax.Hub.Queue([\"Typeset\", MathJax.Hub, wrapper]); MathJax.Hub.Queue(function() { var mjOut = wrapper.getElementsByTagName(\"svg\")[0]; mjOut.setAttribute(\"xmlns\", \"http://www.w3.org/2000/svg\"); output.svg = mjOut.outerHTML; var image = new Image(); image.src = 'data:image/svg+xml;base64,' + window.btoa(unescape(encodeURIComponent(output.svg)));image.onload = function() { var canvas = document.createElement('canvas');canvas.width = image.width;canvas.height = image.height;var context = canvas.getContext('2d');context.drawImage(image, 0, 0);output.img = canvas.toDataURL('image/png');callback(output);};});}";

            //List<string> rendered = new List<string>();

            //Render math
            for (int i = 0; i < RenderStrings.Count; i++)
            {
                string Latex = RenderStrings.ElementAt(i).Key;

                Console.WriteLine("Rendering: {0}/{1}", i + 1, RenderStrings.Count);
                driver.Navigate().Refresh();
                Thread.Sleep(browserWarmUp);
                string jsScript = GenerateJS(Latex);
                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                js.ExecuteScript(jsRender + jsScript);
                Thread.Sleep(browserRender);
                string imgData = ((string)js.ExecuteScript("return window['result'].img;")).Substring(22);
                Console.WriteLine("Recieved {0} chars of data", imgData.Length);
                System.IO.File.WriteAllBytes(imgDir + fileName + ".LaTexExport" + i + ".png", Convert.FromBase64String(imgData));
                //rendered.Add("![LaTex" + i + "](LaTexExport" + i + ".png)");
                //if (mathPointers[i].Item2 == 0) rendered.Add("<div align=\"left\" style=\"text-align:left\"><img src=\"" + "LaTexExport" + i + ".png" + "\"></div>");
                
                //if (mathPointers[i].Item2 == 0) RenderStrings[Latex] = ("![LaTexExport" + i + ".png](" + "LaTexExport" + i + ".png" + ")");
                //if (mathPointers[i].Item2 == 1) RenderStrings[Latex] = ("<div align=\"center\" style=\"text-align:center\"><img src=\"" + "LaTexExport" + i + ".png" + "\"></div>");
                RenderStrings[Latex] = fileName + ".LaTexExport" + i + ".png";
            }

            driver.Close();
            driver.Dispose();

            Console.WriteLine("Compiling...");

            List<string> rendered = new List<string>();
            for (int i = 0; i < mathPointers.Count; i++)
            {
                //rendered.Add(RenderStrings[mathPointers[i].Item3]);
                if (mathPointers[i].Item2 == 0) rendered.Add("![" + fileName + ".LaTexExport" + i + ".png](" + RenderStrings[mathPointers[i].Item3] + ")");
                if (mathPointers[i].Item2 == 1) rendered.Add("<div align=\"center\" style=\"text-align:center\"><img src=\"" + RenderStrings[mathPointers[i].Item3] + "\"></div>");
            }

            List<string> Compiled = new List<string>();

            //Add math back in (Pre rendered)
            for (int i = 0; i < mathPointers.Count; i++)
            {
                Compiled.Add(peicesWithoutMath[i]);
                Compiled.Add(rendered[i]);
            }

            Compiled.Add(peicesWithoutMath[mathPointers.Count]);

            string fileWithMath = string.Join("", Compiled);
            byte[] buff = UTF8Encoding.UTF8.GetBytes(fileWithMath);
            System.IO.File.Create(resFile).Write(buff, 0, buff.Length);
        }

        public static string GenerateJS(string latexExpr)
        {
            string pro = latexExpr.Replace(@"\", @"\\");
            //
            return "window.setTimeout(function(){mj2img(`\\\\[ " + pro + " \\\\]`, function(output){console.log(output); window[\"result\"] = output; });}, 1000);";
        }

        public static string getValidFile()
        {
            Console.Write(">");
            string file = Console.ReadLine();
            if (System.IO.File.Exists(file)) return file;
            Console.WriteLine("That file does not exist!");
            return getValidFile();
        }
    }
}
