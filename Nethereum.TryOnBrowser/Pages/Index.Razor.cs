﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Nethereum.TryOnBrowser.Monaco;
using Nethereum.TryOnBrowser.Modal;

namespace Nethereum.TryOnBrowser.Pages
{
    // Based on https://github.com/Suchiman/Runny all credit to him
    public class IndexModel : ComponentBase
    {
        protected EditorModel editorModel;
        [Inject] ModalService ModalServices { get; set; }

        [Inject] public IJSRuntime JSRuntime { get; set; }        
        protected string Output {get; set;}
        [Inject] private HttpClient Client { get; set; }

        public List<CodeSample> CodeSamples { get; protected set; }
        public int SelectedCodeSample { get; protected set; }

        private CodeSampleRepository _codeSampleRepository;


        protected override void OnInit()
        {
            ModalServices.OnGetContent += OnGetContent;
        }

        public async void OnGetContent(string content, string fileName)
        {
            editorModel.Script = content;
            await Monaco.Interop.EditorSetAsync(JSRuntime, editorModel);

            // create a CodeSample object for this, and point to it
            var tmp = new CodeSample();
            tmp.Code = content;
            tmp.Name = fileName;
            CodeSamples.Add(tmp);

            SelectedCodeSample = CodeSamples.Count -1;
            StateHasChanged();
        }

        protected override async Task OnInitAsync()
        {
            _codeSampleRepository = new CodeSampleRepository(Client);
            CodeSamples = new List<CodeSample>();

            // initialise first empty CodeSample class for "Create New Sample"
            var tmp = new CodeSample();
            tmp.Code = "";
            tmp.Name = "<Load Local .cs Sample>";
            CodeSamples.Add(tmp);
            
            // load remaining code samples from repository
            CodeSamples.AddRange(await _codeSampleRepository.GetCodeSamples());
            SelectedCodeSample = 1;

            editorModel = new EditorModel
            {
                Language = "csharp",
                Script = CodeSamples[SelectedCodeSample].Code
            };

            Compiler.InitializeMetadataReferences(Client);
            await base.OnInitAsync();
        }

        public void Run()
        {
             Compiler.WhenReady(RunInternal);
        }


        public async Task CodeSampleChanged(UIChangeEventArgs evt)
        {
            SelectedCodeSample = Int32.Parse(evt.Value.ToString());
            if (SelectedCodeSample == 0)
            {
                // prompt for file import
                ModalServices.Show("Load File", typeof(ImportForm), ".cs");
                StateHasChanged();
            } else
            {
                editorModel.Script = CodeSamples[SelectedCodeSample].Code;
                await Monaco.Interop.EditorSetAsync(JSRuntime, editorModel);
            }
        }

        public async Task RunInternal()
        {
            Output = "";
            editorModel = await Monaco.Interop.EditorGetAsync(JSRuntime, editorModel);
            Console.WriteLine("Compiling and Running code");

            var sw = Stopwatch.StartNew();

            var currentOut = Console.Out;
            var writer = new StringWriter();
            Console.SetOut(writer);

            Exception exception = null;
            try
            {
                var (success, asm, rawBytes) = Compiler.LoadSource(editorModel.Script, editorModel.Language);

                if (success)
                {
                    var assembly = AppDomain.CurrentDomain.Load(rawBytes);
                    var entry = assembly.EntryPoint;
                    if (entry.Name == "<Main>") // sync wrapper over async Task Main
                    {
                        entry = entry.DeclaringType.GetMethod("Main", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static); // reflect for the async Task Main
                    }
                    var hasArgs = entry.GetParameters().Length > 0;
                    var result = entry.Invoke(null, hasArgs ? new object[] { new string[0] } : null);
                    if (result is Task t)
                    {
                        await t;
                    }
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            Output += writer.ToString();

            if (exception != null)
            {
                Output += "\r\n" + exception.ToString();
            }

            Console.SetOut(currentOut);
            Console.WriteLine("Output " + Output);

            sw.Stop();

            Console.WriteLine("Done in " + sw.ElapsedMilliseconds + "ms");
            StateHasChanged();
        }
    }
}

