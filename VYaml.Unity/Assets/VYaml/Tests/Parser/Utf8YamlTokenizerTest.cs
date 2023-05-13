using System.Buffers;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using VYaml.Parser;

namespace VYaml.Tests
{
    [TestFixture]
    class Utf8YamlTokenizerTest
    {
        [Test]
        public void Empty()
        {
            var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(""));
            var tokenizer = new Utf8YamlTokenizer(sequence);
            Assert.That(tokenizer.Read(), Is.True);
            Assert.That(tokenizer.CurrentTokenType, Is.EqualTo(TokenType.StreamStart));
            Assert.That(tokenizer.Read(), Is.True);
            Assert.That(tokenizer.CurrentTokenType, Is.EqualTo(TokenType.StreamEnd));
            Assert.That(tokenizer.Read(), Is.False);
        }

        [Test]
        [TestCase("a scaler")]
        [TestCase("a:,b")]
        [TestCase(":,b")]
        public void PlainScaler(string scalerValue)
        {
            var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(scalerValue));
            var reader = new Utf8YamlTokenizer(sequence);
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.StreamStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(reader.TakeCurrentTokenContent<Scalar>().ToString(), Is.EqualTo(scalerValue));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.StreamEnd));
            Assert.That(reader.Read(), Is.False);
        }

        [Test]
        public void ExplicitScaler()
        {
            var yaml = string.Join('\n', new[]
            {
                "---",
                "'a scaler'",
                "---",
            });
            var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(yaml));
            var reader = new Utf8YamlTokenizer(sequence);

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.StreamStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.DocumentStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.SingleQuotedScaler));
        }

        [Test]
        public void FlowSequence()
        {
            var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("[item 1, item 2, item 3]"));
            var tokenizer = new Utf8YamlTokenizer(sequence);

            Assert.That(tokenizer.Read(), Is.True);
            Assert.That(tokenizer.CurrentTokenType, Is.EqualTo(TokenType.StreamStart));

            Assert.That(tokenizer.Read(), Is.True);
            Assert.That(tokenizer.CurrentTokenType, Is.EqualTo(TokenType.FlowSequenceStart));

            Assert.That(tokenizer.Read(), Is.True);
            Assert.That(tokenizer.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(tokenizer.TakeCurrentTokenContent<Scalar>().ToString(), Is.EqualTo("item 1"));

            Assert.That(tokenizer.Read(), Is.True);
            Assert.That(tokenizer.CurrentTokenType, Is.EqualTo(TokenType.FlowEntryStart));

            Assert.That(tokenizer.Read(), Is.True);
            Assert.That(tokenizer.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref tokenizer).ToString(), Is.EqualTo("item 2"));

            Assert.That(tokenizer.Read(), Is.True);
            Assert.That(tokenizer.CurrentTokenType, Is.EqualTo(TokenType.FlowEntryStart));

            Assert.That(tokenizer.Read(), Is.True);
            Assert.That(tokenizer.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref tokenizer).ToString(), Is.EqualTo("item 3"));

            Assert.That(tokenizer.Read(), Is.True);
            Assert.That(tokenizer.CurrentTokenType, Is.EqualTo(TokenType.FlowSequenceEnd));

            Assert.That(tokenizer.Read(), Is.True);
            Assert.That(tokenizer.CurrentTokenType, Is.EqualTo(TokenType.StreamEnd));
            Assert.That(tokenizer.Read(), Is.False);
        }

        [Test]
        public void FlowMapping()
        {
            var yaml = string.Join('\n', new[]
            {
                "{",
                "  a simple key: a value, # Note that the KEY token is produced.",
                "  ? a complex key: another value,",
                "}"
            });
            var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(yaml));
            var reader = new Utf8YamlTokenizer(sequence);

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.StreamStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.FlowMappingStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.KeyStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("a simple key"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.ValueStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("a value"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.FlowEntryStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.KeyStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("a complex key"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.ValueStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("another value"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.FlowEntryStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.FlowMappingEnd));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.StreamEnd));

            Assert.That(reader.Read(), Is.False);
        }

        [Test]
        public void BlockSequences()
        {
            var yaml = string.Join('\n', new[]
            {
                "- item 1",
                "- item 2",
                "-",
                "  - item 3.1",
                "  - item 3.2",
                "-",
                "  key 1: value 1",
                "  key 2: value 2",
            });
            var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(yaml));
            var reader = new Utf8YamlTokenizer(sequence);

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.StreamStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockSequenceStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockEntryStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("item 1"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockEntryStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("item 2"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockEntryStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockSequenceStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockEntryStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("item 3.1"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockEntryStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("item 3.2"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockEnd));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockEntryStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockMappingStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.KeyStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("key 1"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.ValueStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("value 1"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.KeyStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("key 2"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.ValueStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("value 2"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockEnd));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockEnd));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.StreamEnd));
            Assert.That(reader.Read(), Is.False);
        }

        [Test]
        public void BlockMappings()
        {
            var yaml = string.Join('\n', new[]
            {
                "a simple key: a value   # The KEY token is produced here.",
                "? a complex key",
                ": another value",
                "a mapping:",
                "  key 1: value 1",
                "  key 2: value 2",
                "a sequence:",
                "  - item 1",
                "  - item 2"
            });
            var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(yaml));
            var reader = new Utf8YamlTokenizer(sequence);

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.StreamStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockMappingStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.KeyStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("a simple key"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.ValueStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("a value"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.KeyStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("a complex key"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.ValueStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("another value"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.KeyStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("a mapping"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.ValueStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockMappingStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.KeyStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("key 1"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.ValueStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("value 1"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.KeyStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("key 2"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.ValueStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("value 2"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockEnd));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.KeyStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("a sequence"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.ValueStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockSequenceStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockEntryStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("item 1"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockEntryStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("item 2"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockEnd));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockEnd));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.StreamEnd));
            Assert.That(reader.Read(), Is.False);
        }

        [Test]
        public void NoBlockSequenceStart()
        {
            var yaml = string.Join('\n', new[]
            {
                 "key:",
                "- item 1",
                "- item 2",
            });
            var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(yaml));
            var reader = new Utf8YamlTokenizer(sequence);

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.StreamStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockMappingStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.KeyStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("key"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.ValueStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockEntryStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("item 1"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockEntryStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("item 2"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockEnd));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.StreamEnd));
            Assert.That(reader.Read(), Is.False);
        }

        [Test]
        public void CollectionsInSequence()
        {
            var yaml = string.Join('\n', new[]
            {
               "- - item 1",
                "  - item 2",
                "- key 1: value 1",
                "  key 2: value 2",
                "- ? complex key",
                "  : complex value",
            });
            var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(yaml));
            var reader = new Utf8YamlTokenizer(sequence);

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.StreamStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockSequenceStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockEntryStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockSequenceStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockEntryStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("item 1"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockEntryStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("item 2"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockEnd));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockEntryStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockMappingStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.KeyStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("key 1"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.ValueStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("value 1"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.KeyStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("key 2"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.ValueStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("value 2"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockEnd));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockEntryStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockMappingStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.KeyStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("complex key"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.ValueStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("complex value"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockEnd));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockEnd));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.StreamEnd));
            Assert.That(reader.Read(), Is.False);
        }

        [Test]
        public void CollectionsInMapping()
        {
            var yaml = string.Join('\n', new[]
            {
                "? a sequence",
                ": - item 1",
                "  - item 2",
                "? a mapping",
                ": key 1: value 1",
                "  key 2: value 2",
            });
            var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(yaml));
            var reader = new Utf8YamlTokenizer(sequence);

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.StreamStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockMappingStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.KeyStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("a sequence"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.ValueStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockSequenceStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockEntryStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("item 1"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockEntryStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("item 2"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockEnd));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.KeyStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("a mapping"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.ValueStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockMappingStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.KeyStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("key 1"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.ValueStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("value 1"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.KeyStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("key 2"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.ValueStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("value 2"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockEnd));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.BlockEnd));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.StreamEnd));
            Assert.That(reader.Read(), Is.False);
        }

        [Test]
        public void SpecEx7_3()
        {
            var yaml = string.Join('\n', new[]
            {
                "{",
                "    ? foo :,",
                "    : bar,",
                "}"
            });
            var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(yaml));
            var reader = new Utf8YamlTokenizer(sequence);

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.StreamStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.FlowMappingStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.KeyStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("foo"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.ValueStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.FlowEntryStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.ValueStart));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("bar"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.FlowEntryStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.FlowMappingEnd));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.StreamEnd));
            Assert.That(reader.Read(), Is.False);
        }

        [Test]
        [Ignore("")]
        public void Mix()
        {
            var yaml = string.Join('\n', new[]
            {
                "- item 1",
                "- item 2",
                "-",
                "  - item 3.1",
                "  - item 3.2",
                "-",
                "  key 1: value 1",
                "  key 2: value 2",
                "  key 3: { a: [{x: 100, y: 200}, {x: 300, y: 400}] }"
            });
            var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(yaml));
            var reader = new Utf8YamlTokenizer(sequence);

        }

        [Test]
        [TestCase(':')]
        [TestCase('?')]
        public void PlainScaler_StartingWithIndicatorInFlow(char literal)
        {
            var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes($"{{a: {literal}b}}"));
            var reader = new Utf8YamlTokenizer(sequence);

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.StreamStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.FlowMappingStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.KeyStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo("a"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.ValueStart));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.PlainScalar));
            Assert.That(Scalar(ref reader).ToString(), Is.EqualTo($"{literal}b"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.FlowMappingEnd));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.CurrentTokenType, Is.EqualTo(TokenType.StreamEnd));
            Assert.That(reader.Read(), Is.False);
        }

        [Test]
        [TestCase("null", ExpectedResult = true)]
        [TestCase("Null", ExpectedResult = true)]
        [TestCase("NULL", ExpectedResult = true)]
        [TestCase("nUll", ExpectedResult = false)]
        [TestCase("null0", ExpectedResult = false)]
        public bool IsNull(string input)
        {
            var sequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(input));
            var tokenizer = new Utf8YamlTokenizer(sequence);
            tokenizer.Read();
            tokenizer.Read();
            return Scalar(ref tokenizer).IsNull();
        }

        static Scalar Scalar(ref Utf8YamlTokenizer tokenizer)
        {
            return tokenizer.TakeCurrentTokenContent<Scalar>();
        }
    }
}
