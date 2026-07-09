// Copyright (c) Facebook, Inc. and its affiliates. All rights reserved.
//
// The examples provided by Facebook are for non-commercial testing and evaluation
// purposes only. Facebook reserves all rights not expressly granted.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// FACEBOOK BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
// ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Text;
using UnityEngine;

public class Json
{
    private StringBuilder sb = new StringBuilder();
    private bool needsComma = false;
    private int indent = 0;
    private bool prettyPrint = false;

    private Json(bool pretty = false)
    {
        prettyPrint = pretty;
        sb.Append("{");
        if (prettyPrint) indent++;
    }

    public static Json Object(bool prettyPrint = false) => new Json(prettyPrint);

    public Json Add(string key, Json value)
    {
        if (value == null) return this;

        AddComma();
        AddIndent();
        sb.Append($"\"{key}\":");

        // Append the provided Json object's serialized content directly.
        // 'value.ToString()' already includes its braces and any internal formatting.
        sb.Append(value.ToString());

        return this;
    }
    public Json Add(string key, string value)
    {
        if (value == null) return this;

        AddComma();
        AddIndent();
        sb.Append($"\"{key}\":\"{EscapeString(value)}\"");
        return this;
    }

    public Json Prop(string key, string value)
    {
        AddComma();
        AddIndent();
        sb.Append($"\"{key}\":\"{EscapeString(value)}\"");
        return this;
    }

    public Json Prop(string key, int value)
    {
        AddComma();
        AddIndent();
        sb.Append($"\"{key}\":{value}");
        return this;
    }

    public Json Prop(string key, float value)
    {
        AddComma();
        AddIndent();
        sb.Append($"\"{key}\":{value}");
        return this;
    }

    public Json Prop(string key, bool value)
    {
        AddComma();
        AddIndent();
        sb.Append($"\"{key}\":{(value ? "true" : "false")}");
        return this;
    }

    public Json Obj(string key, System.Action<Json> builder)
    {
        AddComma();
        AddIndent();
        sb.Append($"\"{key}\":");
        if (prettyPrint) indent++;

        var nested = new Json(prettyPrint);
        builder(nested);
        sb.Append(nested.ToString());

        if (prettyPrint) indent--;
        return this;
    }

    public Json Obj(string key, Json obj)
    {
        AddComma();
        AddIndent();
        sb.Append($"\"{key}\":");

        // Insert the provided Json object's serialized content directly.
        // Respect pretty print by not altering current indent; the nested object already contains its own braces/formatting.
        sb.Append(obj.ToString());

        return this;

    }

    public Json Array(string key, params string[] values)
    {
        AddComma();
        AddIndent();
        sb.Append($"\"{key}\":[");
        for (int i = 0; i < values.Length; i++)
        {
            sb.Append($"\"{EscapeString(values[i])}\"");
            if (i < values.Length - 1) sb.Append(",");
        }
        sb.Append("]");
        return this;
    }

    public Json Array(string key, params int[] values)
    {
        AddComma();
        AddIndent();
        sb.Append($"\"{key}\":[");
        for (int i = 0; i < values.Length; i++)
        {
            sb.Append(values[i]);
            if (i < values.Length - 1) sb.Append(",");
        }
        sb.Append("]");
        return this;
    }

    public Json ArrayOfObjects(string key, System.Action<JsonArray> builder)
    {
        AddComma();
        AddIndent();
        sb.Append($"\"{key}\":");

        var array = new JsonArray(prettyPrint);
        builder(array);
        sb.Append(array.ToString());

        return this;
    }

    private void AddComma()
    {
        if (needsComma)
        {
            sb.Append(",");
            if (prettyPrint) sb.AppendLine();
        }
        needsComma = true;
    }

    private void AddIndent()
    {
        if (prettyPrint)
        {
            for (int i = 0; i < indent; i++)
                sb.Append("  ");
        }
    }

    private string EscapeString(string str)
    {
        return str.Replace("\\", "\\\\")
                  .Replace("\"", "\\\"")
                  .Replace("\n", "\\n")
                  .Replace("\r", "\\r")
                  .Replace("\t", "\\t");
    }

    public override string ToString()
    {
        if (prettyPrint)
        {
            indent--;
            sb.AppendLine();
            for (int i = 0; i < indent; i++)
                sb.Append("  ");
        }
        return sb.ToString() + "}";
    }
}

public class JsonArray
{
    private StringBuilder sb = new StringBuilder();
    private bool needsComma = false;
    private bool prettyPrint = false;

    public JsonArray(bool pretty = false)
    {
        prettyPrint = pretty;
        sb.Append("[");
    }

    public JsonArray Add(System.Action<Json> builder)
    {
        if (needsComma) sb.Append(",");
        needsComma = true;

        var obj = Json.Object(prettyPrint);
        builder(obj);
        sb.Append(obj.ToString());

        return this;
    }

    public JsonArray Add(Json obj)
    {
        if (obj == null) return this;

        if (needsComma) sb.Append(",");
        needsComma = true;

        // Append the serialized JSON object (already includes its braces/formatting).
        sb.Append(obj.ToString());

        return this;

    }

    public override string ToString()
    {
        return sb.ToString() + "]";
    }
}

public class JsonParser
{
    private string json;
    private int pos;

    private JsonParser(string jsonString)
    {
        json = jsonString;
        pos = 0;
    }

    public static JsonValue Parse(string jsonString)
    {
        var parser = new JsonParser(jsonString);
        return parser.ParseValue();
    }

    private JsonValue ParseValue()
    {
        SkipWhitespace();
        if (pos >= json.Length) return null;
        char c = json[pos];
        if (c == '{')
        {
            return ParseObject();
        }
        if (c == '[')
        {
            return ParseArray();
        }
        if (c == '"')
        {
            return ParseString();
        }
        if (c == 't' || c == 'f')
        {
            return ParseBool();
        }
        if (c == 'n')
        {
            return ParseNull();
        }
        if (char.IsDigit(c) || c == '-')
        {
            return ParseNumber();
        }

        return null;
    }

    private JsonValue ParseObject()
    {
        var obj = new JsonValue(JsonValueType.Object);
        pos++; // skip '{'
        SkipWhitespace();

        while (pos < json.Length && json[pos] != '}')
        {
            SkipWhitespace();
            if (json[pos] == '}') break;

            string key = ParseString().AsString();
            SkipWhitespace();
            pos++; // skip ':'
            SkipWhitespace();

            JsonValue value = ParseValue();
            obj.SetProperty(key, value);

            SkipWhitespace();
            if (pos < json.Length && json[pos] == ',') pos++;
        }
        pos++; // skip '}'
        return obj;
    }

    private JsonValue ParseArray()
    {
        var arr = new JsonValue(JsonValueType.Array);
        pos++; // skip '['
        SkipWhitespace();

        while (pos < json.Length && json[pos] != ']')
        {
            SkipWhitespace();
            if (json[pos] == ']') break;

            arr.AddElement(ParseValue());

            SkipWhitespace();
            if (pos < json.Length && json[pos] == ',') pos++;
        }
        pos++; // skip ']'
        return arr;
    }

    private JsonValue ParseString()
    {
        pos++; // skip opening '"'
        var sb = new StringBuilder();

        while (pos < json.Length && json[pos] != '"')
        {
            if (json[pos] == '\\' && pos + 1 < json.Length)
            {
                pos++;
                char escaped = json[pos];
                switch (escaped)
                {
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    default: sb.Append(escaped); break;
                }
            }
            else
            {
                sb.Append(json[pos]);
            }
            pos++;
        }
        pos++; // skip closing '"'
        return new JsonValue(sb.ToString());
    }

    private JsonValue ParseNumber()
    {
        int start = pos;
        if (json[pos] == '-') pos++;

        while (pos < json.Length && (char.IsDigit(json[pos]) || json[pos] == '.' || json[pos] == 'e' || json[pos] == 'E' || json[pos] == '+' || json[pos] == '-'))
        {
            // For scientific notation: only allow e/E after digits, and +/- only after e/E
            char c = json[pos];
            if (c == 'e' || c == 'E')
            {
                // 'e' or 'E' must come after a digit
                if (pos == start || (!char.IsDigit(json[pos - 1]) && json[pos - 1] != '.'))
                    break;
            }
            else if (c == '+' || c == '-')
            {
                // '+' or '-' in the middle must come after 'e' or 'E'
                if (pos == start) { pos++; continue; } // leading minus already handled
                if (json[pos - 1] != 'e' && json[pos - 1] != 'E')
                    break;
            }
            pos++;
        }

        string numStr = json.Substring(start, pos - start);
        if (numStr.Contains(".") || numStr.Contains("e") || numStr.Contains("E"))
        {
            if (double.TryParse(numStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double d))
                return new JsonValue((float)d);
        }
        else
        {
            if (int.TryParse(numStr, out int i))
                return new JsonValue(i);
            if (long.TryParse(numStr, out long l))
                return new JsonValue(l);
        }
        SDKDebug.LogError($"ParseNumber failed to parse: '{numStr}'");
        return new JsonValue(0);
    }

    private JsonValue ParseBool()
    {
        if (pos + 4 <= json.Length && json.Substring(pos, 4) == "true")
        {
            pos += 4;
            return new JsonValue(true);
        }
        if (pos + 5 <= json.Length && json.Substring(pos, 5) == "false")
        {
            pos += 5;
            return new JsonValue(false);
        }
        return new JsonValue(false);
    }

    private JsonValue ParseNull()
    {
        if (pos + 4 <= json.Length && json.Substring(pos, 4) == "null")
        {
            pos += 4;
            return new JsonValue(JsonValueType.Null);
        }
        SDKDebug.LogError($"ParseNull failed at position {pos}: expected 'null' but found '{json.Substring(pos, System.Math.Min(4, json.Length - pos))}'");
        return null;
    }

    private void SkipWhitespace()
    {
        while (pos < json.Length && char.IsWhiteSpace(json[pos]))
            pos++;
    }
}

public enum JsonValueType
{
    Null,
    String,
    Number,
    Bool,
    Object,
    Array
}

public class JsonValue
{
    public JsonValueType Type { get; private set; }
    private string stringValue;
    private float floatValue;
    private long intValue;
    private bool boolValue;
    private System.Collections.Generic.Dictionary<string, JsonValue> objectValue;
    private System.Collections.Generic.List<JsonValue> arrayValue;

    public JsonValue(JsonValueType type)
    {
        Type = type;
        if (type == JsonValueType.Object)
            objectValue = new System.Collections.Generic.Dictionary<string, JsonValue>();
        if (type == JsonValueType.Array)
            arrayValue = new System.Collections.Generic.List<JsonValue>();
    }

    public JsonValue(string value) { Type = JsonValueType.String; stringValue = value; }
    public JsonValue(int value) { Type = JsonValueType.Number; intValue = value; floatValue = value; }
    public JsonValue(long value) { Type = JsonValueType.Number; intValue = value; floatValue = value; }
    public JsonValue(float value) { Type = JsonValueType.Number; floatValue = value; intValue = (long)value; }
    public JsonValue(bool value) { Type = JsonValueType.Bool; boolValue = value; }

    public void SetProperty(string key, JsonValue value)
    {
        if (objectValue == null)
            objectValue = new System.Collections.Generic.Dictionary<string, JsonValue>();
        objectValue[key] = value;
    }

    public void AddElement(JsonValue value)
    {
        if (arrayValue == null)
            arrayValue = new System.Collections.Generic.List<JsonValue>();
        arrayValue.Add(value);
    }

    public JsonValue this[string key]
    {
        get
        {
            if (Type == JsonValueType.Object && objectValue != null && objectValue.ContainsKey(key))
                return objectValue[key];
            return null;
        }
    }

    public JsonValue this[int index]
    {
        get
        {
            if (Type == JsonValueType.Array && arrayValue != null && index >= 0 && index < arrayValue.Count)
                return arrayValue[index];
            return null;
        }
    }

    public string AsString() => stringValue ?? (Type == JsonValueType.Number ? floatValue.ToString() : "");
    public int AsInt() => (int)intValue;
    public long AsLong() => intValue;
    public float AsFloat() => floatValue;
    public bool AsBool() => boolValue;
    public bool IsNull => Type == JsonValueType.Null;

    public int Count
    {
        get
        {
            if (Type == JsonValueType.Array && arrayValue != null) return arrayValue.Count;
            if (Type == JsonValueType.Object && objectValue != null) return objectValue.Count;
            return 0;
        }
    }

    public bool HasKey(string key)
    {
        return Type == JsonValueType.Object && objectValue != null && objectValue.ContainsKey(key);
    }

    public System.Collections.Generic.IEnumerable<string> Keys
    {
        get
        {
            if (Type == JsonValueType.Object && objectValue != null)
                return objectValue.Keys;
            return new string[0];
        }
    }

    public System.Collections.Generic.IEnumerable<JsonValue> ArrayElements
    {
        get
        {
            if (Type == JsonValueType.Array && arrayValue != null)
                return arrayValue;
            return new JsonValue[0];
        }
    }

    public JsonValue GetPath(string path)
    {
        string[] parts = path.Split('.');
        JsonValue current = this;

        foreach (string part in parts)
        {
            if (current == null) return null;

            if (part.Contains("[") && part.Contains("]"))
            {
                int bracketStart = part.IndexOf('[');
                int bracketEnd = part.IndexOf(']');
                string key = part.Substring(0, bracketStart);
                string indexStr = part.Substring(bracketStart + 1, bracketEnd - bracketStart - 1);

                if (!string.IsNullOrEmpty(key))
                    current = current[key];

                if (int.TryParse(indexStr, out int index))
                    current = current[index];
            }
            else
            {
                current = current[part];
            }
        }
        return current;
    }

    public override string ToString()
    {
        switch (Type)
        {
            case JsonValueType.String: return $"\"{stringValue}\"";
            case JsonValueType.Number: return floatValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
            case JsonValueType.Bool: return boolValue ? "true" : "false";
            case JsonValueType.Null: return "null";
            case JsonValueType.Object:
                var objSb = new StringBuilder("{");
                bool firstObj = true;
                foreach (var kvp in objectValue)
                {
                    if (!firstObj) objSb.Append(",");
                    objSb.Append($"\"{kvp.Key}\":{kvp.Value}");
                    firstObj = false;
                }
                objSb.Append("}");
                return objSb.ToString();
            case JsonValueType.Array:
                var arrSb = new StringBuilder("[");
                bool firstArr = true;
                foreach (var elem in arrayValue)
                {
                    if (!firstArr) arrSb.Append(",");
                    arrSb.Append(elem.ToString());
                    firstArr = false;
                }
                arrSb.Append("]");
                return arrSb.ToString();
            default: return "";
        }
    }

    public string ToStringTrimmed()
    {
        string beforeTrim = this.ToString();
        return beforeTrim.Trim('"');
    }
}
