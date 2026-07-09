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

using NUnit.Framework;

namespace Meta.InstantGames.Tests
{
    [TestFixture]
    public class JsonParserTests
    {
        [Test]
        public void Parse_SimpleObject_ReturnsCorrectValues()
        {
            string json = "{\"name\":\"test\",\"value\":42}";
            JsonValue result = JsonParser.Parse(json);

            Assert.IsNotNull(result);
            Assert.AreEqual("test", result["name"].AsString());
            Assert.AreEqual(42, result["value"].AsInt());
        }

        [Test]
        public void Parse_NestedObject_ReturnsCorrectValues()
        {
            string json = "{\"outer\":{\"inner\":\"value\"}}";
            JsonValue result = JsonParser.Parse(json);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result["outer"]);
            Assert.AreEqual("value", result["outer"]["inner"].AsString());
        }

        [Test]
        public void Parse_Array_ReturnsCorrectCount()
        {
            string json = "{\"items\":[1,2,3]}";
            JsonValue result = JsonParser.Parse(json);

            Assert.IsNotNull(result);
            Assert.AreEqual(3, result["items"].Count);
            Assert.AreEqual(1, result["items"][0].AsInt());
            Assert.AreEqual(2, result["items"][1].AsInt());
            Assert.AreEqual(3, result["items"][2].AsInt());
        }

        [Test]
        public void Parse_BooleanValues_ReturnsCorrectValues()
        {
            string json = "{\"isTrue\":true,\"isFalse\":false}";
            JsonValue result = JsonParser.Parse(json);

            Assert.IsNotNull(result);
            Assert.IsTrue(result["isTrue"].AsBool());
            Assert.IsFalse(result["isFalse"].AsBool());
        }

        [Test]
        public void Parse_NullValue_ReturnsNull()
        {
            string json = "{\"nullField\":null}";
            JsonValue result = JsonParser.Parse(json);

            Assert.IsNotNull(result);
            Assert.IsTrue(result["nullField"].IsNull);
        }

        [Test]
        public void Parse_FloatValue_ReturnsCorrectValue()
        {
            string json = "{\"pi\":3.14159}";
            JsonValue result = JsonParser.Parse(json);

            Assert.IsNotNull(result);
            Assert.AreEqual(3.14159f, result["pi"].AsFloat(), 0.00001f);
        }

        [Test]
        public void Parse_StringWithEscapedCharacters_ReturnsCorrectValue()
        {
            string json = "{\"text\":\"line1\\nline2\"}";
            JsonValue result = JsonParser.Parse(json);

            Assert.IsNotNull(result);
            Assert.AreEqual("line1\nline2", result["text"].AsString());
        }

        [Test]
        public void Parse_EmptyObject_ReturnsEmptyJsonValue()
        {
            string json = "{}";
            JsonValue result = JsonParser.Parse(json);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void Parse_EmptyArray_ReturnsEmptyJsonValue()
        {
            string json = "{\"items\":[]}";
            JsonValue result = JsonParser.Parse(json);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result["items"].Count);
        }

        [Test]
        public void GetPath_SimpleKey_ReturnsValue()
        {
            string json = "{\"name\":\"test\"}";
            JsonValue result = JsonParser.Parse(json);

            Assert.AreEqual("test", result.GetPath("name").AsString());
        }

        [Test]
        public void GetPath_NestedKey_ReturnsValue()
        {
            string json = "{\"outer\":{\"inner\":\"value\"}}";
            JsonValue result = JsonParser.Parse(json);

            Assert.AreEqual("value", result.GetPath("outer.inner").AsString());
        }

        [Test]
        public void GetPath_ArrayIndex_ReturnsValue()
        {
            string json = "{\"items\":[\"a\",\"b\",\"c\"]}";
            JsonValue result = JsonParser.Parse(json);

            Assert.AreEqual("b", result.GetPath("items[1]").AsString());
        }

        [Test]
        public void HasKey_ExistingKey_ReturnsTrue()
        {
            string json = "{\"exists\":true}";
            JsonValue result = JsonParser.Parse(json);

            Assert.IsTrue(result.HasKey("exists"));
        }

        [Test]
        public void HasKey_NonExistingKey_ReturnsFalse()
        {
            string json = "{\"exists\":true}";
            JsonValue result = JsonParser.Parse(json);

            Assert.IsFalse(result.HasKey("notExists"));
        }

        [Test]
        public void Parse_NegativeNumber_ReturnsCorrectValue()
        {
            string json = "{\"negative\":-42}";
            JsonValue result = JsonParser.Parse(json);

            Assert.AreEqual(-42, result["negative"].AsInt());
        }

        [Test]
        public void Parse_LongNumber_ReturnsCorrectValue()
        {
            string json = "{\"big\":9999999999}";
            JsonValue result = JsonParser.Parse(json);

            Assert.AreEqual(9999999999L, result["big"].AsLong());
        }

        [Test]
        public void Keys_ReturnsAllKeys()
        {
            string json = "{\"a\":1,\"b\":2,\"c\":3}";
            JsonValue result = JsonParser.Parse(json);

            var keys = new System.Collections.Generic.List<string>(result.Keys);
            Assert.AreEqual(3, keys.Count);
            Assert.Contains("a", keys);
            Assert.Contains("b", keys);
            Assert.Contains("c", keys);
        }

        [Test]
        public void ArrayElements_ReturnsAllElements()
        {
            string json = "{\"items\":[1,2,3]}";
            JsonValue result = JsonParser.Parse(json);

            var elements = new System.Collections.Generic.List<JsonValue>(result["items"].ArrayElements);
            Assert.AreEqual(3, elements.Count);
        }
    }

    [TestFixture]
    public class JsonBuilderTests
    {
        [Test]
        public void Object_WithStringProp_CreatesValidJson()
        {
            string json = Json.Object()
                .Prop("name", "test")
                .ToString();

            JsonValue parsed = JsonParser.Parse(json);
            Assert.AreEqual("test", parsed["name"].AsString());
        }

        [Test]
        public void Object_WithIntProp_CreatesValidJson()
        {
            string json = Json.Object()
                .Prop("value", 42)
                .ToString();

            JsonValue parsed = JsonParser.Parse(json);
            Assert.AreEqual(42, parsed["value"].AsInt());
        }

        [Test]
        public void Object_WithFloatProp_CreatesValidJson()
        {
            string json = Json.Object()
                .Prop("pi", 3.14f)
                .ToString();

            JsonValue parsed = JsonParser.Parse(json);
            Assert.AreEqual(3.14f, parsed["pi"].AsFloat(), 0.01f);
        }

        [Test]
        public void Object_WithBoolProp_CreatesValidJson()
        {
            string json = Json.Object()
                .Prop("active", true)
                .ToString();

            JsonValue parsed = JsonParser.Parse(json);
            Assert.IsTrue(parsed["active"].AsBool());
        }

        [Test]
        public void Object_WithNestedObject_CreatesValidJson()
        {
            string json = Json.Object()
                .Obj("nested", n => n.Prop("inner", "value"))
                .ToString();

            JsonValue parsed = JsonParser.Parse(json);
            Assert.AreEqual("value", parsed["nested"]["inner"].AsString());
        }

        [Test]
        public void Object_WithStringArray_CreatesValidJson()
        {
            string json = Json.Object()
                .Array("tags", "a", "b", "c")
                .ToString();

            JsonValue parsed = JsonParser.Parse(json);
            Assert.AreEqual(3, parsed["tags"].Count);
            Assert.AreEqual("a", parsed["tags"][0].AsString());
        }

        [Test]
        public void Object_WithIntArray_CreatesValidJson()
        {
            string json = Json.Object()
                .Array("numbers", 1, 2, 3)
                .ToString();

            JsonValue parsed = JsonParser.Parse(json);
            Assert.AreEqual(3, parsed["numbers"].Count);
            Assert.AreEqual(2, parsed["numbers"][1].AsInt());
        }

        [Test]
        public void Object_WithArrayOfObjects_CreatesValidJson()
        {
            string json = Json.Object()
                .ArrayOfObjects("items", arr => arr
                    .Add(obj => obj.Prop("id", 1))
                    .Add(obj => obj.Prop("id", 2)))
                .ToString();

            JsonValue parsed = JsonParser.Parse(json);
            Assert.AreEqual(2, parsed["items"].Count);
            Assert.AreEqual(1, parsed["items"][0]["id"].AsInt());
            Assert.AreEqual(2, parsed["items"][1]["id"].AsInt());
        }

        [Test]
        public void Object_EscapesSpecialCharacters()
        {
            string json = Json.Object()
                .Prop("text", "line1\nline2")
                .ToString();

            JsonValue parsed = JsonParser.Parse(json);
            Assert.AreEqual("line1\nline2", parsed["text"].AsString());
        }

        [Test]
        public void Object_EscapesQuotes()
        {
            string json = Json.Object()
                .Prop("quote", "He said \"hello\"")
                .ToString();

            JsonValue parsed = JsonParser.Parse(json);
            Assert.AreEqual("He said \"hello\"", parsed["quote"].AsString());
        }
    }

    [TestFixture]
    public class JsonValueTests
    {
        [Test]
        public void JsonValue_StringType_ReturnsCorrectType()
        {
            var value = new JsonValue("test");
            Assert.AreEqual(JsonValueType.String, value.Type);
        }

        [Test]
        public void JsonValue_IntType_ReturnsCorrectType()
        {
            var value = new JsonValue(42);
            Assert.AreEqual(JsonValueType.Number, value.Type);
        }

        [Test]
        public void JsonValue_BoolType_ReturnsCorrectType()
        {
            var value = new JsonValue(true);
            Assert.AreEqual(JsonValueType.Bool, value.Type);
        }

        [Test]
        public void JsonValue_NullType_ReturnsCorrectType()
        {
            var value = new JsonValue(JsonValueType.Null);
            Assert.AreEqual(JsonValueType.Null, value.Type);
            Assert.IsTrue(value.IsNull);
        }

        [Test]
        public void JsonValue_SetProperty_AddsProperty()
        {
            var obj = new JsonValue(JsonValueType.Object);
            obj.SetProperty("key", new JsonValue("value"));

            Assert.AreEqual("value", obj["key"].AsString());
        }

        [Test]
        public void JsonValue_AddElement_AddsToArray()
        {
            var arr = new JsonValue(JsonValueType.Array);
            arr.AddElement(new JsonValue(1));
            arr.AddElement(new JsonValue(2));

            Assert.AreEqual(2, arr.Count);
            Assert.AreEqual(1, arr[0].AsInt());
            Assert.AreEqual(2, arr[1].AsInt());
        }

        [Test]
        public void JsonValue_ToString_Object_ReturnsValidJson()
        {
            var obj = new JsonValue(JsonValueType.Object);
            obj.SetProperty("name", new JsonValue("test"));

            string result = obj.ToString();
            Assert.IsTrue(result.Contains("\"name\""));
            Assert.IsTrue(result.Contains("\"test\""));
        }

        [Test]
        public void JsonValue_ToString_Array_ReturnsValidJson()
        {
            var arr = new JsonValue(JsonValueType.Array);
            arr.AddElement(new JsonValue(1));
            arr.AddElement(new JsonValue(2));

            string result = arr.ToString();
            Assert.AreEqual("[1,2]", result);
        }

        [Test]
        public void JsonValue_IndexerString_NonExistingKey_ReturnsNull()
        {
            var obj = new JsonValue(JsonValueType.Object);
            Assert.IsNull(obj["nonexistent"]);
        }

        [Test]
        public void JsonValue_IndexerInt_OutOfRange_ReturnsNull()
        {
            var arr = new JsonValue(JsonValueType.Array);
            arr.AddElement(new JsonValue(1));

            Assert.IsNull(arr[5]);
            Assert.IsNull(arr[-1]);
        }
    }
}
