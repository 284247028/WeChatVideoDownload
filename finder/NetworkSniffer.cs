﻿using Fiddler;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Telerik.NetworkConnections.Windows;
using static System.Collections.Specialized.BitVector32;

namespace finder
{

    public class NetworkSniffer
    {


        // 使用 EventHandler<T> 泛型委托，其中 T 是自定义的 EventArgs 类
        public event EventHandler<VideoDataEventArgs> DataUpdated;

        protected virtual void OnDataUpdated(VideoData videoData)
        {
            // 创建 VideoDataEventArgs 的实例，并触发事件
            DataUpdated?.Invoke(this, new VideoDataEventArgs(videoData));
        }

        public event EventHandler<VideoKeyDataEventArgs> DataKeyUpdated;

        protected virtual void OnDataKeyUpdated(VideoKeyData videoKeyData)
        {
            // 创建 VideoDataEventArgs 的实例，并触发事件
            DataKeyUpdated?.Invoke(this, new VideoKeyDataEventArgs(videoKeyData));
        }

        public bool InstallRootCert()
        {
            string rootCertificatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RootCertificate.p12");
            string rootCertificatePassword = "S0m3T0pS3cr3tP4ssw1rd";
            BCCertMaker.BCCertMaker certProvider = new BCCertMaker.BCCertMaker();
            CertMaker.oCertProvider = certProvider;

            // 先检查本地证书文件是否存在
            if (File.Exists(rootCertificatePath))
            {
                // 从文件读取证书,读取后 再去检测 证书是否被信任之类.
                certProvider.ReadRootCertificateAndPrivateKeyFromPkcs12File(rootCertificatePath, rootCertificatePassword);
                // 检查系统中是否已经安装并信任该证书
                if (!CertMaker.rootCertIsTrusted())
                {
                    if (!CertMaker.trustRootCert())
                        return false;
                }
            }
            else
            {
                // 如果本地没有证书文件，则检查系统中是否已存在证书
                if (!CertMaker.rootCertExists())
                {
                    // 创建新的根证书
                    if (!CertMaker.createRootCert())
                        return false;

                    // 将新创建的证书写入文件
                    certProvider.WriteRootCertificateAndPrivateKeyToPkcs12File(rootCertificatePath, rootCertificatePassword);

                    // 信任根证书
                    if (!CertMaker.trustRootCert())
                        return false;
                }
            }

            return true;  // 证书已正确安装并信任
        }

        public void Start()
        {



            // 设置FiddlerCore的配置
            FiddlerCoreStartupSettings startupSettings = new FiddlerCoreStartupSettingsBuilder()
                .RegisterAsSystemProxy()
                .DecryptSSL()
                .Build();

            //证书这个,提供一次就行了.不要多次,否则就又变空了.
            //BCCertMaker.BCCertMaker certProvider = new BCCertMaker.BCCertMaker();
            //CertMaker.oCertProvider = certProvider;

            // 启动FiddlerCore
            FiddlerApplication.Startup(startupSettings);

            // 注册事件处理函数
            FiddlerApplication.BeforeRequest += OnBeforeRequest;
            FiddlerApplication.BeforeResponse += OnBeforeResponse;

            Console.WriteLine("FiddlerCore started.");
        }

        public void Stop()
        {
            try
            {
                // 注销事件处理函数
                FiddlerApplication.BeforeRequest -= OnBeforeRequest;
                FiddlerApplication.BeforeResponse -= OnBeforeResponse;

                // 关闭FiddlerCore
                FiddlerApplication.Shutdown();
                Console.WriteLine("FiddlerCore stopped.");
            }
            catch (Exception)
            {

                //throw;
            }

        }
        byte[] all = null;
        private async void OnBeforeRequest(Session oSession)
        {

            // oSession["x-OverrideSslProtocols"] = "tls1.0;tls1.1;tls1.2";
            if (!oSession.isHTTPS) return;

            var myurl = oSession.fullUrl;

            //去掉zip压缩
            if (oSession.RequestMethod == "GET"&&
                (myurl.ToLower().EndsWith("/worker_release.js")
                ||myurl.ToLower().Contains("feeddetail.publish")
                ||myurl.ToLower().Contains("index.publish")
                ||myurl.ToLower().Contains("virtual_svg-icons-register.publish")))
            {
                oSession.bBufferResponse = true;
                oSession.oRequest.headers.Remove("Accept-Encoding");
                // oSession.oRequest.headers.Add("Accept-Encoding", "gzip"); // Uncomment if you want to explicitly allow gzip.
            }

            if (oSession.RequestMethod == "POST" && myurl.ToLower().Contains("wx.qq.com/my_worker_release"))
            {
                await Task.Delay(1000);
                try
                {
                    byte[] decArray = oSession.requestBodyBytes;



                    if (decArray.Length==131072)
                    {
                        all = (byte[])decArray.Clone();
                        // var  all2 = ((byte[])(all.Reverse())).Clone();
                        string videoUrl = videoDownloadUrl; // Assume this retrieves the URL stored earlier
                        videoDownloadUrl="";
                        if (videoUrl!="")
                        {
                            var videoKey = new VideoKeyData(videoUrl, decArray);
                            OnDataKeyUpdated(videoKey);
                        }


                        //progress.Report(videoKey);
                    }
                    else
                    {

                    }

                    oSession.utilCreateResponseAndBypassServer();
                    oSession.oResponse.headers.HTTPResponseCode = 200;
                    oSession.oResponse.headers.HTTPResponseStatus = "200 OK";
                    oSession.oResponse.headers["Access-Control-Allow-Origin"] = "*";
                    oSession.oResponse.headers["Access-Control-Allow-Headers"] = "*";
                    oSession.oResponse.headers["Access-Control-Allow-Methods"] = "OPTIONS, POST, GET";
                    oSession.utilSetResponseBody("<html><body>success!</body></html>");
                }
                catch
                {

                }
            }
            if (oSession.RequestMethod == "POST" && myurl.ToLower().Contains("wx.qq.com/my_index.publish"))
            {
                var json = oSession.GetRequestBodyAsString();
                var videoData = VideoManager.ParseVideoDataFromJson(json);
                videoDownloadUrl=videoData.Url;
                OnDataUpdated(videoData);


                oSession.utilCreateResponseAndBypassServer();
                oSession.oResponse.headers.HTTPResponseCode = 200;
                oSession.oResponse.headers.HTTPResponseStatus = "200 OK";
                oSession.oResponse.headers["Access-Control-Allow-Origin"] = "*";
                oSession.oResponse.headers["Access-Control-Allow-Headers"] = "*";
                oSession.oResponse.headers["Access-Control-Allow-Methods"] = "OPTIONS, POST, GET";
                oSession.utilSetResponseBody("<html><body>success!</body></html>");
            }
            if (oSession.RequestMethod == "POST" && myurl.ToLower().Contains("wx.qq.com/my_feeddetail"))
            {
                var json = oSession.GetRequestBodyAsString();

                oSession.utilCreateResponseAndBypassServer();
                oSession.oResponse.headers.HTTPResponseCode = 200;
                oSession.oResponse.headers.HTTPResponseStatus = "200 OK";
                oSession.oResponse.headers["Access-Control-Allow-Origin"] = "*";
                oSession.oResponse.headers["Access-Control-Allow-Headers"] = "*";
                oSession.oResponse.headers["Access-Control-Allow-Methods"] = "OPTIONS, POST, GET";
                oSession.utilSetResponseBody("<html><body>success!</body></html>");
            }

            if (oSession.RequestMethod == "POST" && myurl.ToLower().Contains("wx.qq.com/my_virtual_svg-icons-register.publish"))
            {
                var json = oSession.GetRequestBodyAsString();

                var videoData = VideoManager.ParseVideoDataFromJson(json);
                // videoDownloadUrl=videoData.Url;
                OnDataUpdated(videoData);


                oSession.utilCreateResponseAndBypassServer();
                oSession.oResponse.headers.HTTPResponseCode = 200;
                oSession.oResponse.headers.HTTPResponseStatus = "200 OK";
                oSession.oResponse.headers["Access-Control-Allow-Origin"] = "*";
                oSession.oResponse.headers["Access-Control-Allow-Headers"] = "*";
                oSession.oResponse.headers["Access-Control-Allow-Methods"] = "OPTIONS, POST, GET";
                oSession.utilSetResponseBody("<html><body>success!</body></html>");
            }

        }

        private static string videoDownloadUrl = "";

        private void OnBeforeResponse(Session session)
        {

            if (!session.isHTTPS) return;

            string myurl = session.fullUrl;

            // 检查请求类型是否为 GET 并且 URL 结尾是否符合特定模式
            if (session.RequestMethod == "GET" && myurl.ToLower().EndsWith("/worker_release.js"))
            {
                session.utilDecodeResponse(); // 确保响应已解码
                string replacementScript = @"
setTimeout(function() {
    var rr=$2.reverse();
var limitedRR = Array.from(rr.slice(0, 10)); 
var hexStrs = limitedRR.map(byte => byte.toString(16).padStart(2, '0')).join(' ');
console.log(hexStrs); 
console.log(limitedRR); 
    fetch('https://wx.qq.com/my_worker_release', {
        mode: 'no-cors', // 设置为 ""no-cors"" 以避开 CORS 策略
        method: 'POST',
        headers: {'Content-Type': 'application/octet-stream'},
        body: rr,
    }).then(response => {
        console.log(response.ok, response.body)
    });
    $1.decryptor_array.set(rr);
}, 1000);"; // 使用 setTimeout 延迟 3 秒执行
                replacementScript = @"

    var rr=$2.reverse();
var limitedRR = Array.from(rr.slice(0, 10)); 
var hexStrs = limitedRR.map(byte => byte.toString(16).padStart(2, '0')).join(' ');
console.log(hexStrs); 
console.log(limitedRR); 
    fetch('https://wx.qq.com/my_worker_release', {
        mode: 'no-cors', // 设置为 ""no-cors"" 以避开 CORS 策略
        method: 'POST',
        headers: {'Content-Type': 'application/octet-stream'},
        body: rr,
    }).then(response => {
        console.log(response.ok, response.body)
    });
    $1.decryptor_array.set(rr);
"; // 使用 setTimeout 延迟 3 秒执行
                string responseBody = session.GetResponseBodyAsString();
                // 在这里你可能需要将 replacementScript 插入到 responseBody 中的适当位置


                // 使用正则表达式修改响应体中的 JavaScript
                responseBody = Regex.Replace(responseBody, @"(\w)\.decryptor_array\.set\((\w)\.reverse\(\)\)", replacementScript);
                session.utilSetResponseBody(responseBody);
            }
            if (false&& session.RequestMethod == "GET" && myurl.ToLower().Contains("feeddetail.publish"))
            {
                // 确保响应已解码
                session.utilDecodeResponse();
                string responseBody = session.GetResponseBodyAsString();

                // 定位 "function po(e" 第一次出现的位置
                int index = responseBody.IndexOf("function po(e");
                if (index != -1)
                {
                    // 注入的 onMounted 生命周期钩子的 JavaScript 代码
                    string codeToInject = @"
    // 使用 setInterval 而不是 setTimeout 来每隔一定时间重复执行代码
    setTimeout(() => { 
        const feedData = Se().feed; // 获取数据
        console.log(feedData); 
        // 发送数据到指定地址
        fetch('https://wx.qq.com/my_feeddetail', {
        mode: 'no-cors', // 设置为 ""no-cors"" 以避开 CORS 策略
            method: 'POST',
            headers: {
                'Content-Type': 'application/json' // 确保使用正确的内容类型
            },
            body: JSON.stringify({
                feed: feedData // 确保发送的数据格式正确
            })
        })
        .then(response => response.text())
        .then(data => {
            console.log('Data received:', data); // 处理服务器返回的数据
        })
        .catch(err => {
            console.error('Error sending data:', err); // 错误处理
        });
    }, 1000);"; // 1000 毫秒，即每隔一秒执行一次

                    // 计算插入点：确保不会产生负索引


                    // 在找到的位置后插入代码
                    responseBody = responseBody+ codeToInject;

                    // 将修改后的内容写回会话
                    session.utilSetResponseBody(responseBody);
                }
            }
            // 特定 URL 处理
            if (myurl.ToLower().Contains("finder.video.qq.com/251/20302"))
            {
                if (session.RequestMethod == "HEAD")
                {
                    if (myurl!=videoDownloadUrl)
                    {

                    }
                    var temp = videoDownloadUrl;

                    //替换部分内容
                    ///  var temp2=myurl.Replace()

                    videoDownloadUrl = myurl; // 存储 URL 供其他部分使用
                }
                // 添加跨域访问控制响应头
                session.oResponse.headers.Add("Access-Control-Allow-Origin", "*");
                session.oResponse.headers.Add("Access-Control-Allow-Headers", "*");
                session.oResponse.headers.Add("Access-Control-Allow-Methods", "OPTIONS, POST, GET");
            }

            if (false&& myurl.ToLower().Contains("index.publish"))
            {  // 确保响应已解码
                session.utilDecodeResponse();
                string responseBody = session.GetResponseBodyAsString();


                // 注入的 onMounted 生命周期钩子的 JavaScript 代码
                string codeToInject = @"
// 立即执行函数，用于封装代码，防止污染全局命名空间
window.mygetback = function () {
    // 检查一个标记是否已经设置，如果设置则直接返回，避免重复执行
    if (window.wvds !== undefined) {
        return
    }
    // 定义接收服务器的URL
    let receiver_url = ""wx.qq.com/my_index.publish"";
    // 定义一个函数，用于处理视频数据的发送
    function send_response_if_is_video(response) {
        // 如果响应未定义，直接返回
        if (response == undefined) {
            return;
        }
        // 如果响应中的错误消息不是期待的值，则返回
        if (response[""err_msg""] != ""H5ExtTransfer:ok"") {
            return;
        }
        // 解析响应中的 JSON 数据
        let value = JSON.parse(response[""jsapi_resp""][""resp_json""]);
        // 检查必要的数据路径是否存在，若不存在则返回
        if (value[""object""] == undefined || value[""object""][""object_desc""] == undefined  || value[""object""][""object_desc""][""media""].length == 0) {
            return
        }
        // 获取媒体数据的第一个元素
        let media = value[""object""][""object_desc""][""media""][0];
        // 构造要发送的视频数据对象
        let video_data = {
            ""decode_key"": media[""decode_key""], // 解码键
            ""url"": media[""url""] + media[""url_token""], // 视频的完整URL
            ""size"": media[""file_size""], // 文件大小
            ""description"":  value[""object""][""object_desc""][""description""].trim(), // 描述文本
            ""uploader"": value[""object""][""nickname""] // 上传者昵称
        }
        // 使用 fetch API 发送 POST 请求到服务器
        fetch(receiver_url, {
            method: 'POST',
            mode: 'no-cors', // CORS 模式设置为 no-cors
            body: JSON.stringify(video_data), // 将视频数据转化为 JSON 字符串作为请求体
        }).then((resp) => {
            // 请求完成后，在控制台输出日志
            console.log(`video data for ${video_data[""description""]} sent`);
        });
    }
    // 包装函数，用于拦截原有的函数调用
    function wrapper(name, origin) {
        // 输出注入日志
        console.log(`injecting ${name}`);
        // 返回一个新的函数
        return function() {
            let cmdName = arguments[0];
            // 检查参数长度是否为3，符合特定调用模式
            if (arguments.length == 3) {
                let original_callback = arguments[2]; // 保存原始回调函数
                arguments[2] = async function () { // 替换原始回调函数
                    if (arguments.length == 1) {
                        send_response_if_is_video(arguments[0]); // 调用处理视频数据的函数
                    }
                    return await original_callback.apply(this, arguments); // 调用原始回调函数
                }
            }
            let result = origin.apply(this, arguments); // 调用原始函数
            return result;
        }
    }
    // 输出服务启动的日志
    console.log(`------- Invoke WechatVideoDownloader Service ---------`);
    // 使用包装函数覆盖原有的 WeixinJSBridge.invoke 方法
    window.WeixinJSBridge.invoke = wrapper(""WeixinJSBridge.invoke"", window.WeixinJSBridge.invoke);
    // 设置标记，避免重复执行
    window.wvds = true;
}
mygetback();
";
                // 在找到的位置后插入代码
                responseBody = responseBody+ codeToInject;

                // 将修改后的内容写回会话
                session.utilSetResponseBody(responseBody);


            }

            if (myurl.ToLower().Contains("virtual_svg-icons-register.publish"))
            {  // 确保响应已解码
                session.utilDecodeResponse();
                string responseBody = session.GetResponseBodyAsString();


                // 注入的 onMounted 生命周期钩子的 JavaScript 代码
                string codeToInject = @"
    async finderGetCommentDetail(n) {
        const feedResult = await this.post({
            name: ""FinderGetCommentDetail"",
            data: {
                finderBasereq: {
                    ...this.finderBasereq,
                    exptFlag: 1,
                    requestId: jn()
                },
                platformScene: 2,
                ...n
            }
        });
        var data_object = feedResult.data.object;
        var media = data_object.objectDesc.media[0];
        var fetch_body = {
            //duration: media.spec[0].durationMs,
            //title: data_object.objectDesc.description,
            //url: media.url + media.urlToken,
            //size: media.fileSize,
            //key: media.decodeKey
  //""decode_key"": media[""decode_key""], // 解码键
  //          ""url"": media[""url""] + media[""url_token""], // 视频的完整URL
  //          ""size"": media[""file_size""], // 文件大小
  //          ""description"":  value[""object""][""object_desc""][""description""].trim(), // 描述文本
  //          ""uploader"": value[""object""][""nickname""] // 上传者昵称

  ""decode_key"": media.decodeKey, // 解码键
            ""url"": media.url + media.urlToken, // 视频的完整URL
            ""size"": media.fileSize, // 文件大小
            ""description"": data_object.objectDesc.description, // 描述文本
            ""uploader"": data_object.nickname // 上传者昵称



        };
        fetch('wx.qq.com/my_virtual_svg-icons-register.publish', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(fetch_body)
        }).then(response=>{
            console.log(response.ok, response.body)
        }
        );
        return feedResult;
    }
";
                string old = "async finderGetCommentDetail(n){return this.post({name:\"FinderGetCommentDetail\",data:{finderBasereq:{...this.finderBasereq,exptFlag:1,requestId:jn()},platformScene:2,...n}})}";

                // 在找到的位置后插入代码
                responseBody = responseBody.Replace(old, codeToInject);

                // 将修改后的内容写回会话
                session.utilSetResponseBody(responseBody);


            }
        }
    }

}
