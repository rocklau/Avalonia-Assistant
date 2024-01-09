﻿using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Desktop.Assistant.Services;
using Desktop.Assistant.Domain.MessageTypes;
using Desktop.Assistant.Domain.Models;
using Desktop.Assistant.Models;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using System.Reactive.Linq;
using Desktop.Assistant.Domain.Model;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Net.Http;
using Desktop.Assistant.Domain.Utils;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Data;
using System.Reflection.Metadata;
using Microsoft.SemanticKernel.Planning.Handlebars;
using Desktop.Assistant.Domain.NativePlugins;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

namespace Desktop.Assistant.ViewModels
{
    public class ChatViewModel : ViewModelBase
    {        
        public ObservableCollection<MessageBase> Messages { get; private set; }
        private Kernel kernel;

        public string NewMessageContent
        {
            get => newMessageContent;
            set => this.RaiseAndSetIfChanged(ref newMessageContent, value);
        }
        
        public  ICommand DictateMessageCommand { get; private set; }

        public ICommand AttachImageCommand { get; private set; }

        public ICommand SendMessageCommand { get; private set; }

        public ICommand EnterKeyPressedCommand { get; private set; }

        public ChatViewModel(ChatService chatService, RoutingState router) : base(router)
        {
            this.Messages = new ObservableCollection<MessageBase>();

            canSendMessage = this.WhenAnyValue(x => x.NewMessageContent).Select(x => !string.IsNullOrEmpty(x));

            SendMessageCommand = ReactiveCommand.CreateFromTask(SendMessage, canSendMessage);
            AttachImageCommand = ReactiveCommand.CreateFromTask(AttachImage);
            DictateMessageCommand = ReactiveCommand.CreateFromTask(DictateMessage);
            EnterKeyPressedCommand = ReactiveCommand.CreateFromTask(EnterKeyPressed);

            var handler = new OpenAIHttpClientHandler();
            kernel = Kernel.CreateBuilder()
                .AddOpenAIChatCompletion(
                  modelId: OpenAIOption.Model,
            apiKey: OpenAIOption.Key,
                  httpClient: new HttpClient(handler)
                     )
                .Build();

        }

        async Task EnterKeyPressed()
        {
            await SendMessage();
        }

        async Task SendMessage()
        {
        
            //OpenAIChatCompletionService chatCompletionService = new(OpenAIOption.Model, OpenAIOption.Key, httpClient: new HttpClient(handler));
            //var msg=await chatCompletionService.GetChatMessageContentAsync(NewMessageContent); 
            var planner = new HandlebarsPlanner(
               new HandlebarsPlannerOptions()
               {
                   AllowLoops = true
               });
             kernel.ImportPluginFromObject(new WindowsPlugin(kernel), "WindowsPlugin");

            var plan = await planner.CreatePlanAsync(kernel, NewMessageContent);

            var msg = await plan.InvokeAsync(kernel);

            this.Messages.Add(new TextMessage(NewMessageContent) { Role = ChatRoleType.Sender });
            this.Messages.Add(new TextMessage(msg) { Role = ChatRoleType.Receiver });
            NewMessageContent = string.Empty;
        }

        async Task AttachImage()
        {

        }

        async Task DictateMessage()
        {
        }


        //Fields
        private ChatService chatService;
        private string newMessageContent;
        private WindowNotificationManager windowNotificationManager;
        private IObservable<bool> canSendMessage;
    }
}
