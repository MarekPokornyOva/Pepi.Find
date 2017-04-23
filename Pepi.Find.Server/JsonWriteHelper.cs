#region using
using Newtonsoft.Json;
using System;
#endregion using

namespace Pepi.Find.Server
{
	static class JsonWriteHelper
	{
		public static JsonWriter WriteProperty(this JsonWriter writer,string name,string value)
		{
			writer.WritePropertyName(name,true);
			writer.WriteValue(value);
			return writer;
		}

		public static JsonWriter WriteProperty(this JsonWriter writer,string name,bool value)
		{
			writer.WritePropertyName(name,true);
			writer.WriteValue(value);
			return writer;
		}

		public static JsonWriter WriteProperty(this JsonWriter writer,string name,int value)
		{
			writer.WritePropertyName(name,true);
			writer.WriteValue(value);
			return writer;
		}

		public static JsonWriter WriteProperty(this JsonWriter writer,string name,long value)
		{
			writer.WritePropertyName(name,true);
			writer.WriteValue(value);
			return writer;
		}

		public static JsonWriter WritePropertyNull(this JsonWriter writer,string name)
		{
			writer.WritePropertyName(name,true);
			writer.WriteNull();
			return writer;
		}

		public static IDisposable WriteObject(this JsonWriter writer)
		{
			writer.WriteStartObject();
			return new Disp(() => writer.WriteEndObject());
		}

		public static IDisposable WriteObject(this JsonWriter writer,string name)
		{
			writer.WritePropertyName(name,true);
			return WriteObject(writer);
		}

		public static IDisposable WriteArray(this JsonWriter writer,string name)
		{
			writer.WritePropertyName(name,true);
			writer.WriteStartArray();
			return new Disp(() => writer.WriteEndArray());
		}

		class Disp:IDisposable
		{
			Action _dispAction;

			internal Disp(Action dispAction)
			{
				_dispAction=dispAction;
			}

			public void Dispose()
			{
				_dispAction();
			}
		}
	}
}
