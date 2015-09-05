﻿// Copyright(C) 2015 by Pavel Osadchuk <xakz.pc@gmail.com>

//  This file is part of Xakpc.TelegramBot.Client.

//  Xakpc.TelegramBot.Client is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  Xakpc.TelegramBot.Client is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with Xakpc.TelegramBot.Client. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Deserializers;
using Xakpc.TelegramBot.Model;

namespace Xakpc.TelegramBot.Client
{
    public class TelegramBotApiClient : ITelegramBotApiClient
    {
        private const string Url = "https://api.telegram.org/bot{0}/{1}";
        private readonly RestClient _client;
        private readonly string _token;

        public TelegramBotApiClient(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentNullException(nameof(token), "Your authtoken must be valid");
            }

            _token = token;
            _client = new RestClient(@"https://api.telegram.org/");
        }

        #region ITelegramBotClient implementation

        public Task<User> GetMeAsync()
        {
            var request = new RestRequest(MakeRequest("getMe"), Method.GET);
            return ExecuteGetRequestAsync<User>(request);
        }

        public async Task<List<Update>> GetUpdatesAsync(int offset = 0, int limit = 100, int timeout = 0)
        {
            var request = new RestRequest(MakeRequest("getUpdates"), Method.GET);

            if (offset != 0)
                request.AddQueryIntParameter("offset", offset);

            if (limit != 0)
                request.AddQueryIntParameter("limit", limit);

            if (timeout != 100)
                request.AddQueryIntParameter("timeout", timeout);

            var uri = _client.BuildUri(request);
            var sb = new StringBuilder();

            // long-polling implementation
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite);

                var requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
                using (var response = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead))
                {
                    using (var body = await response.Content.ReadAsStreamAsync())
                    using (var reader = new StreamReader(body))
                        while (!reader.EndOfStream)
                            sb.Append(reader.ReadLine());
                }
            }

            var deserial = new JsonDeserializer();
            var apiResponse = deserial.Deserialize<ApiResponse<List<Update>>>(new RestResponse {Content = sb.ToString()});

            return apiResponse.Result;
        }

        public async Task SetWebhookAsync(Uri url)
        {
            if (url.Scheme != "https")
                throw new ArgumentException("Url must be HTTPS", nameof(url));

            var request = new RestRequest(MakeRequest("setWebhook"), Method.GET);
            request.AddQueryParameter("url", url.AbsoluteUri);
            var result = await _client.ExecuteGetTaskAsync(request).ConfigureAwait(false);
        }

        public async Task RemoveWebhookAsync()
        {
            var request = new RestRequest(MakeRequest("setWebhook"), Method.GET);
            request.AddQueryParameter("url", string.Empty);
            var result = await _client.ExecuteGetTaskAsync(request).ConfigureAwait(false);
        }

        public Task<Message> SendMessageAsync(int chatId, string text, bool? disableWebPagePreview,
            int? replyToMessageId, ReplyMarkup replyMarkup)
        {
            var request = new RestRequest(MakeRequest("sendMessage"), Method.POST);

            request.AddQueryIntParameter("chat_id", chatId);

            if (disableWebPagePreview.HasValue)
                request.AddQueryParameter("disable_web_page_preview", disableWebPagePreview.Value.ToString());

            if (replyToMessageId.HasValue)
                request.AddQueryIntParameter("reply_to_message_id", replyToMessageId.Value);

            request.AddParameter("text", text);

            if (replyMarkup != null)
                request.AddParameter("reply_markup", JsonConvert.SerializeObject(replyMarkup));

            return ExecutePostRequestAsync<Message>(request);
        }

        public Task<Message> ForwardMessageAsync(int chatId, int fromChatId, int messageId)
        {
            var request = new RestRequest(MakeRequest("forwardMessage"), Method.GET);

            request.AddQueryIntParameter("chat_id", chatId);
            request.AddQueryIntParameter("from_chat_id", fromChatId);
            request.AddQueryIntParameter("message_id", messageId);

            return ExecuteGetRequestAsync<Message>(request);
        }

        public Task<Message> SendPhotoAsync(int chatId, InputFile photo, string caption, int? replyToMessageId,
            ReplyMarkup replyMarkup)
        {
            var request = new PhotoRestRequest(chatId, MakeRequest("sendPhoto"))
            {
                Caption = caption,
                ReplyToMessageId = replyToMessageId,
                ReplyMarkup = replyMarkup
            };

            request.AddMedia("photo", photo);

            return ExecutePostRequestAsync<Message>(request);
        }

        public Task<Message> SendPhotoAsync(int chatId, string photo, string caption, int? replyToMessageId,
            ReplyMarkup replyMarkup)
        {
            var request = new PhotoRestRequest(chatId, MakeRequest("sendPhoto"))
            {
                Caption = caption,
                ReplyToMessageId = replyToMessageId,
                ReplyMarkup = replyMarkup
            };

            request.AddMedia("photo", photo);

            return ExecutePostRequestAsync<Message>(request);
        }

        public Task<Message> SendAudioAsync(int chatId, InputFile audio, int? replyToMessageId, ReplyMarkup replyMarkup)
        {
            var request = new MediaRestRequest(chatId, MakeRequest("sendAudio"))
            {
                ReplyToMessageId = replyToMessageId,
                ReplyMarkup = replyMarkup
            };

            request.AddMedia("audio", audio);

            return ExecutePostRequestAsync<Message>(request);
        }

        public Task<Message> SendAudioAsync(int chatId, string audio, int? replyToMessageId, ReplyMarkup replyMarkup)
        {
            var request = new MediaRestRequest(chatId, MakeRequest("sendAudio"))
            {
                ReplyToMessageId = replyToMessageId,
                ReplyMarkup = replyMarkup
            };

            request.AddMedia("audio", audio);

            return ExecutePostRequestAsync<Message>(request);
        }

        public Task<Message> SendDocumentAsync(int chatId, InputFile document, int? replyToMessageId,
            ReplyMarkup replyMarkup)
        {
            var request = new MediaRestRequest(chatId, MakeRequest("sendDocument"))
            {
                ReplyToMessageId = replyToMessageId,
                ReplyMarkup = replyMarkup
            };

            request.AddMedia("document", document);

            return ExecutePostRequestAsync<Message>(request);
        }

        public Task<Message> SendDocumentAsync(int chatId, string document, int? replyToMessageId,
            ReplyMarkup replyMarkup)
        {
            var request = new MediaRestRequest(chatId, MakeRequest("sendDocument"))
            {
                ReplyToMessageId = replyToMessageId,
                ReplyMarkup = replyMarkup
            };

            request.AddMedia("document", document);

            return ExecutePostRequestAsync<Message>(request);
        }

        public Task<Message> SendStickerAsync(int chatId, InputFile sticker, int? replyToMessageId,
            ReplyMarkup replyMarkup)
        {
            var request = new MediaRestRequest(chatId, MakeRequest("sendSticker"))
            {
                ReplyToMessageId = replyToMessageId,
                ReplyMarkup = replyMarkup
            };

            request.AddMedia("sticker", sticker);

            return ExecutePostRequestAsync<Message>(request);
        }

        public Task<Message> SendStickerAsync(int chatId, string sticker, int? replyToMessageId, ReplyMarkup replyMarkup)
        {
            var request = new MediaRestRequest(chatId, MakeRequest("sendSticker"))
            {
                ReplyToMessageId = replyToMessageId,
                ReplyMarkup = replyMarkup
            };

            request.AddMedia("sticker", sticker);

            return ExecutePostRequestAsync<Message>(request);
        }

        public Task<Message> SendVideoAsync(int chatId, InputFile video, int? replyToMessageId, ReplyMarkup replyMarkup)
        {
            var request = new MediaRestRequest(chatId, MakeRequest("sendVideo"))
            {
                ReplyToMessageId = replyToMessageId,
                ReplyMarkup = replyMarkup
            };

            request.AddMedia("video", video);

            return ExecutePostRequestAsync<Message>(request);
        }

        public Task<Message> SendVideoAsync(int chatId, string video, int? replyToMessageId, ReplyMarkup replyMarkup)
        {
            var request = new MediaRestRequest(chatId, MakeRequest("sendVideo"))
            {
                ReplyToMessageId = replyToMessageId,
                ReplyMarkup = replyMarkup
            };

            request.AddMedia("video", video);

            return ExecutePostRequestAsync<Message>(request);
        }

        public Task<Message> SendLocationAsync(int chatId, float latitude, float longitude, int? replyToMessageId,
            ReplyMarkup replyMarkup)
        {
            var request = new MediaRestRequest(chatId, MakeRequest("sendLocation"))
            {
                ReplyToMessageId = replyToMessageId,
                ReplyMarkup = replyMarkup
            };

            request.AddMedia("latitude", latitude.ToString("F6"));
            request.AddMedia("longitude", longitude.ToString("F6"));

            return ExecutePostRequestAsync<Message>(request);
        }

        public Task<bool> SendChatAction(int chatId, string action)
        {
            var request = new RestRequest(MakeRequest("sendChatAction"), Method.GET);

            request.AddQueryIntParameter("chat_id", chatId);
            request.AddQueryParameter("action", action);

            return ExecuteGetRequestAsync<bool>(request);
        }

        public Task<UserProfilePhotos> GetUserProfilePhotos(int userId, int? offset, int? limit)
        {
            var request = new RestRequest(MakeRequest("sendChatAction"), Method.GET);

            request.AddQueryIntParameter("user_id", userId);

            if (offset.HasValue)
                request.AddQueryIntParameter("offset", offset.Value);

            if (limit.HasValue)
            {
                ThrowOutOfRangeExceptionIfNotInRange("limit", limit.Value, 1, 100);
                request.AddQueryIntParameter("limit", limit.Value);
            }

            return ExecuteGetRequestAsync<UserProfilePhotos>(request);
        }

        #endregion

        #region Private Classes

        private class MediaRestRequest : RestRequest
        {
            public MediaRestRequest(int chatId, string api) : base(api, Method.POST)
            {
                AddQueryParameter("chat_id", chatId.ToString());
            }

            public int? ReplyToMessageId
            {
                set
                {
                    if (value.HasValue)
                        AddQueryParameter("reply_to_message_id", value.Value.ToString());
                }
            }

            public ReplyMarkup ReplyMarkup
            {
                set
                {
                    if (value != null)
                        AddParameter("reply_markup", JsonConvert.SerializeObject(value));
                }
            }

            public void AddMedia(string mediaParameter, InputFile media)
            {
                AddFile(mediaParameter, media.Data, media.FileName);
            }

            public void AddMedia(string mediaParameter, string media)
            {
                AddQueryParameter(mediaParameter, media);
            }
        }

        private class PhotoRestRequest : MediaRestRequest
        {
            public PhotoRestRequest(int chatId, string api) : base(chatId, api)
            {
            }

            public string Caption
            {
                set
                {
                    if (!string.IsNullOrEmpty(value))
                        AddQueryParameter("caption", value);
                }
            }
        }

        #endregion

        #region Private Methods

        private async Task<T> ExecutePostRequestAsync<T>(IRestRequest request)
        {
            var result = await _client.ExecutePostTaskAsync<ApiResponse<T>>(request).ConfigureAwait(false);

            if (result.ResponseStatus != ResponseStatus.Completed)
                throw new Exception("Transport exception: " + result.ResponseStatus, result.ErrorException);

            if (result.StatusCode != HttpStatusCode.OK)
                throw new HttpRequestException("Http error: " + result.StatusCode);

            if (!result.Data.Ok)
                throw new Exception("API error");

            return result.Data.Result;
        }

        private async Task<T> ExecuteGetRequestAsync<T>(IRestRequest request)
        {
            var result = await _client.ExecuteGetTaskAsync<ApiResponse<T>>(request).ConfigureAwait(false);

            if (result.ResponseStatus != ResponseStatus.Completed)
                throw new Exception("Transport exception: " + result.ResponseStatus, result.ErrorException);

            if (result.StatusCode != HttpStatusCode.OK)
                throw new HttpRequestException("Http error: " + result.StatusCode);

            if (!result.Data.Ok)
                throw new Exception("API error");

            return result.Data.Result;
        }

        protected static void ThrowOutOfRangeExceptionIfNotInRange(string param, int value, int @from, int to)
        {
            if ((value < @from) || (value > to))
                throw new ArgumentOutOfRangeException(param,
                    $"Argument must be in {@from} – {(to == int.MaxValue ? "∞" : to.ToString())} range");
        }

        private string MakeRequest(string method)
        {
            return $"bot{_token}/{method}";
        }

        #endregion
    }
}