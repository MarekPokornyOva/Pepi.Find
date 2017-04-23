#region using
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Security.Principal;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Configuration;
using Pepi.Find.Server.Abstract;
using Pepi.Find.Server;
using Pepi.Find.SqlRepository;
#endregion using

namespace Pepi.Find.WinDesktop
{
	public partial class Form1:Form
	{
		#region .ctor
		HttpListener _listener;
		volatile bool _stopping;
		DefaultRequestHandler _handler;
		Queue<Message> _queue = new Queue<Message>();

		public Form1()
		{
			InitializeComponent();
		}
		#endregion .ctor

		#region component event handlers
		private void Form1_Load(object sender,EventArgs e)
		{
			WindowsPrincipal pricipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
			bool hasAdministrativeRight = pricipal.IsInRole(WindowsBuiltInRole.Administrator);

			buttonStart.Enabled=hasAdministrativeRight;
			buttonElevate.Enabled=!hasAdministrativeRight;
			buttonElevate.Visible=!hasAdministrativeRight;

			textBoxConnectionString.Text=ConfigurationManager.AppSettings["connectionString"];
			textBoxBindings.Text=ConfigurationManager.AppSettings["bindings"];
		}

		private void buttonElevate_Click(object sender,EventArgs e)
		{
			ProcessStartInfo startInfo = new ProcessStartInfo();
			startInfo.UseShellExecute=true;
			startInfo.WorkingDirectory=Environment.CurrentDirectory;
			startInfo.FileName=Application.ExecutablePath;
			startInfo.Verb="runas";
			Process p = Process.Start(startInfo);
			this.Close();
		}

		private void buttonStart_Click(object sender,EventArgs e)
		{
			_handler=new DefaultRequestHandler(
				 textBoxConnectionString.Text.StartsWith("Null;",StringComparison.InvariantCultureIgnoreCase)
				 ? (IIndexRepository)new NullIndexRepository()
				 : new SqlIndexRepository(textBoxConnectionString.Text));

			_listener=new HttpListener();
			foreach (string item in textBoxBindings.Text.Split(',',';'))
				_listener.Prefixes.Add(item);
			_listener.Start();

			buttonStart.Enabled=false;
			buttonStop.Enabled=true;
			buttonTest.Enabled=true;
			_stopping=false;

			BeginGetContext();
		}

		private void buttonStop_Click(object sender,EventArgs e)
		{
			buttonStart.Enabled=true;
			buttonStop.Enabled=false;
			buttonTest.Enabled=false;

			_stopping=true;
			_listener.Stop();
		}

		private void timer1_Tick(object sender,EventArgs e)
		{
			listViewMessages.SuspendLayout();
			int a = 0;
			while ((_queue.Count!=0)&&((a++)<1000))
			{
				Message m = _queue.Dequeue();
				ListViewItem lvi = listViewMessages.Items.Add(m.Timestamp);
				lvi.SubItems.Add(m.Url);
				lvi.Tag=m;
				if (listViewMessages.Items.Count==1)
					listViewMessages.Items[0].Selected=true;
			}
			listViewMessages.ResumeLayout();
		}

		private void listViewMessages_SelectedIndexChanged(object sender,EventArgs e)
		{
			ListView.SelectedListViewItemCollection slvic = listViewMessages.SelectedItems;
			Message m = slvic.Count==0 ? null : slvic[0].Tag as Message;
			if (m==null)
			{
				textBoxRequest.Text="";
				textBoxResponse.Text="";
			}
			else
			{
				textBoxRequest.Text=$"{m.HttpMethod} {m.Url}{Environment.NewLine}{Environment.NewLine}{m.RequestHeaders}{(m.RequestBody.Length>4096 ? $"Long message: {m.RequestBody.Length}{Environment.NewLine}{m.RequestBody.Substring(0,4096)}" : m.RequestBody)}";
				textBoxResponse.Text=$"{m.HttpStatusCode}{Environment.NewLine}{Environment.NewLine}{m.ResponseHeaders}{(m.ResponseBody.Length>4096 ? $"Long message: {m.ResponseBody.Length}{Environment.NewLine}{m.ResponseBody.Substring(0,4096)}" : m.ResponseBody)}";
			}
		}

		private void buttonTest_Click(object sender,EventArgs e)
		{
			if (_listener?.Prefixes==null)
				return;

			byte[] data = Encoding.UTF8.GetBytes(@"{""size"":3,""query"":{""filtered"":{""query"":{""constant_score"":{""filter"":{""and"":[{""or"":[{""not"":{""filter"":{""term"":{""___types"":""EPiServer.Core.IContent""}}}},{""term"":{""IsDeleted$$bool"":false}}]},{""or"":[{""and"":[{""not"":{""filter"":{""term"":{""___types"":""EPiServer.Core.ILocalizable""}}}},{""not"":{""filter"":{""term"":{""___types"":""EPiServer.Core.IVersionable""}}}}]},{""and"":[{""term"":{""___types"":""EPiServer.Core.ILocalizable""}},{""not"":{""filter"":{""term"":{""___types"":""EPiServer.Core.IVersionable""}}}},{""term"":{""Language.Name$$string"":""fi""}}]},{""and"":[{""not"":{""filter"":{""term"":{""___types"":""EPiServer.Core.ILocalizable""}}}},{""term"":{""___types"":""EPiServer.Core.IVersionable""}},{""and"":[{""term"":{""Status"":4}},{""range"":{""StartPublishedNormalized$$date"":{""from"":""0001-01-01T00:00:00Z"",""to"":""2099-12-31T23:59:59Z"",""include_lower"":true,""include_upper"":true}}},{""or"":[{""not"":{""filter"":{""exists"":{""field"":""StopPublish$$date""}}}},{""range"":{""StopPublish$$date"":{""from"":""2099-12-31T23:59:59Z"",""include_lower"":false}}}]}]}]},{""and"":[{""or"":[{""not"":{""filter"":{""exists"":{""field"":""PublishedInLanguage.fi.StopPublish$$date""}}}},{""range"":{""PublishedInLanguage.fi.StopPublish$$date"":{""from"":""2099-12-31T23:59:59Z"",""include_lower"":false}}}]},{""range"":{""PublishedInLanguage.fi.StartPublish$$date"":{""from"":""0001-01-01T00:00:00Z"",""to"":""2099-12-31T23:59:59Z"",""include_lower"":true,""include_upper"":true}}}]}]},{""or"":[{""not"":{""filter"":{""term"":{""___types"":""EPiServer.Security.IContentSecurable""}}}},{""term"":{""UsersWithReadAccess$$string.lowercase"":""admin""}},{""terms"":{""RolesWithReadAccess$$string"":[""WebEditors"",""WebAdmins"",""Administrators"",""Everyone""]}}]},{""or"":[{""not"":{""filter"":{""exists"":{""field"":""SiteId$$string""}}}},{""term"":{""SiteId$$string"":""01234567-89ab-cdef-0123-456789abcd""}}]},{""term"":{""TestProp$$bool"":false}},{""term"":{""___types"":""EPiServer.Core.IContent""}}]}}},""filter"":{""term"":{""___types"":""TestPage""}}}},""sort"":[{""StartPublish$$date"":{""order"":""desc"",""ignore_unmapped"":true}}],""fields"":[""___types"",""ContentLink.ID$$number"",""ContentLink.ProviderName$$string"",""Language.Name$$string""]}");

			HttpWebRequest req = System.Net.WebRequest.CreateHttp(_listener.Prefixes.First().Replace("*","test")+"test-index-name/_search");
			req.ContentType="application/json";
			req.UserAgent="EPiServer-Find-NET-API/12.2.4.0";
			req.ContentLength=data.Length;
			req.Method="POST";
			req.Timeout=10*60*1000;
			using (Stream stream = req.GetRequestStream())
				stream.Write(data,0,data.Length);
			using (System.Net.WebResponse res = req.GetResponse())
			{ }
		}

		private void listViewMessages_KeyPress(object sender,KeyPressEventArgs e)
		{
			listViewMessages.SuspendLayout();
			ListView.SelectedListViewItemCollection items = listViewMessages.SelectedItems;
			foreach (ListViewItem item in items)
			{
				Message m = item.Tag as Message;
				if (m!=null)
				{
					m.RequestBody=null;
					m.ResponseBody=null;
				}
				listViewMessages.Items.Remove(item);
			}
			listViewMessages.ResumeLayout();
		}
		#endregion component event handlers

		#region HTTP listening root method
		void BeginGetContext()
		{
			_listener.BeginGetContext(new AsyncCallback(async ar =>
			{
				if (_stopping)
					return;
				BeginGetContext();

				HttpListenerContext ctx = _listener.EndGetContext(ar);
				MemoryStream msIn = new MemoryStream();
				MemoryStream msOut = new MemoryStream();
				await ctx.Request.InputStream.CopyToAsync(msIn);
				msIn.Position=0;

				Message m = new Message { Timestamp=DateTime.Now.ToString(),HttpMethod=ctx.Request.HttpMethod,Url=ctx.Request.RawUrl,RequestHeaders=ctx.Request.Headers.ToString(),RequestBody=new StreamReader(msIn).ReadToEnd(),HttpStatusCode="",ResponseHeaders="",ResponseBody="" };
				_queue.Enqueue(m);
				msIn.Position=0;

				await _handler.ProcessRequestAsync(new WebContext(ctx,msIn,new StreamWriteDistributor(ctx.Response.OutputStream,msOut)));

				m.HttpStatusCode=ctx.Response.StatusCode.ToString();
				m.ResponseHeaders=ctx.Response.Headers.ToString();
				msOut.Position=0;
				m.ResponseBody=new StreamReader(msOut).ReadToEnd();
				msOut.Dispose();
			}),null);
		}
		#endregion HTTP listening root method

		#region Message
		class Message
		{
			internal string Timestamp;
			internal string Url;
			internal string HttpMethod;
			internal string HttpStatusCode;
			internal string RequestHeaders;
			internal string RequestBody;
			internal string ResponseHeaders;
			internal string ResponseBody;
		}
		#endregion Message
	}

	#region WebContext
	public class WebContext:IHandlerContext
	{
		public IHandlerRequest Request => _request;
		public IHandlerResponse Response => _response;

		IHandlerRequest _request;
		IHandlerResponse _response;
		public WebContext(HttpListenerContext context,Stream inputStream,Stream outputStream)
		{
			_request=new WebRequest(context.Request,inputStream);
			_response=new WebResponse(context.Response,outputStream);
		}
	}

	class WebRequest:IHandlerRequest
	{
		HttpListenerRequest _request;
		Stream _inputStream;
		internal WebRequest(HttpListenerRequest request,Stream inputStream)
		{
			_request=request;
			_headers=new LateProperty<IDictionary<string,string[]>>(() => new Dictionary<string,string[]>());
			_inputStream=inputStream;
		}

		readonly LateProperty<IDictionary<string,string[]>> _headers;

		public string RawUrl => _request.RawUrl;
		public string UserAgent => _request.Headers["User-Agent"];
		public IDictionary<string,string[]> Headers => _headers.Value;
		public Stream Body => _inputStream;
	}

	class WebResponse:IHandlerResponse
	{
		private HttpListenerResponse _response;
		Stream _outputStream;
		public WebResponse(HttpListenerResponse response,Stream outputStream)
		{
			_response=response;
			_outputStream=outputStream;
		}

		public Stream Body => _outputStream;
		public int StatusCode { get => _response.StatusCode; set => _response.StatusCode=value; }
	}

	#region LateProperty
	class LateProperty<T>
	{
		Func<T> _getter;
		internal LateProperty(Func<T> getter)
		{
			_getter=getter;
		}

		bool _loaded;
		T _value;
		internal T Value
		{
			get
			{
				if (!_loaded)
				{
					_loaded=true;
					_value=_getter();
				}
				return _value;
			}
		}
	}
	#endregion LateProperty
	#endregion WebContext

	#region StreamWriteDistributor
	class StreamWriteDistributor:Stream
	{
		Stream _primary, _other;
		internal StreamWriteDistributor(Stream primary,Stream other)
		{
			_primary=primary;
			_other=other;
		}

		public override bool CanRead => false;

		public override bool CanSeek => false;

		public override bool CanWrite => true;

		public override long Length => _primary.Length;

		public override long Position { get => _primary.Position; set => _primary.Position=value; }

		public override void Flush() { _primary.Flush(); _other.Flush(); }
		public override int Read(byte[] buffer,int offset,int count) => _primary.Read(buffer,offset,count);
		public override long Seek(long offset,SeekOrigin origin) => _primary.Seek(offset,origin);
		public override void SetLength(long value) => _primary.SetLength(value);
		public override void Write(byte[] buffer,int offset,int count) { _primary.Write(buffer,offset,count); _other.Write(buffer,offset,count); }

		protected override void Dispose(bool disposing) { if (disposing) _primary.Dispose(); }
	}
	#endregion StreamWriteDistributor
}
