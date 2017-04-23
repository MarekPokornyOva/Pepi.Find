#region using
using System.IO;
using Newtonsoft.Json;
using System.Reflection;
#endregion using

namespace Pepi.Find.Server
{
	internal class MultiRootJsonTextReader:JsonTextReader
	{
		//TextReader _reader;
		public MultiRootJsonTextReader(TextReader reader) : base(reader)
		{
			//_reader = reader;
		}

		public override bool Read()
		{
            if (CurrentState == State.Finished)
            {
                //CurrentState = State.Start;
                //internal JsonReader.State _currentState;
                typeof(JsonReader).GetTypeInfo().GetDeclaredField("_currentState")
                    .SetValue(this, State.Start);
                //_reader.Peek() should return "-1" end the end of content
            }
			return base.Read();
		}
	}
}