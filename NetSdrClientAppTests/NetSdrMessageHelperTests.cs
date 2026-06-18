using NetSdrClientApp.Messages;

namespace NetSdrClientAppTests
{
    public class NetSdrMessageHelperTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void GetControlItemMessageTest()
        {
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.Ack;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverState;
            int parametersLength = 7500;

            //Act
            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, code, new byte[parametersLength]);

            var headerBytes = msg.Take(2);
            var codeBytes = msg.Skip(2).Take(2);
            var parametersBytes = msg.Skip(4);

            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);
            var actualCode = BitConverter.ToInt16(codeBytes.ToArray());

            //Assert
            Assert.Multiple(() =>
            {
                Assert.That(headerBytes.Count(), Is.EqualTo(2));
                Assert.That(msg, Has.Length.EqualTo(actualLength));
                Assert.That(type, Is.EqualTo(actualType));
                Assert.That(actualCode, Is.EqualTo((short)code));
                Assert.That(parametersBytes.Count(), Is.EqualTo(parametersLength));
            });
        }

        [Test]
        public void GetDataItemMessageTest()
        {
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.DataItem2;
            int parametersLength = 7500;

            //Act
            byte[] msg = NetSdrMessageHelper.GetDataItemMessage(type, new byte[parametersLength]);

            var headerBytes = msg.Take(2);
            var parametersBytes = msg.Skip(2);

            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.That(headerBytes.Count(), Is.EqualTo(2));
                Assert.That(msg, Has.Length.EqualTo(actualLength));
                Assert.That(type, Is.EqualTo(actualType));
                Assert.That(parametersBytes.Count(), Is.EqualTo(parametersLength));
            });
        }

        [Test]
        public void GetControlItemMessage_LengthIsCorrect()
        {
            var msg = NetSdrMessageHelper.GetControlItemMessage(
                NetSdrMessageHelper.MsgTypes.SetControlItem,
                NetSdrMessageHelper.ControlItemCodes.ReceiverFrequency,
                new byte[6]);

            Assert.That(msg, Has.Length.EqualTo(10)); // 2 header + 2 code + 6 params
        }

        [Test]
        public void GetControlItemMessage_TypeIsEncodedCorrectly()
        {
            var type = NetSdrMessageHelper.MsgTypes.SetControlItem;
            var msg = NetSdrMessageHelper.GetControlItemMessage(
                type,
                NetSdrMessageHelper.ControlItemCodes.ReceiverState,
                new byte[4]);

            var num = BitConverter.ToUInt16(msg.Take(2).ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);

            Assert.That(actualType, Is.EqualTo(type));
        }

        [Test]
        public void GetDataItemMessage_EmptyParams_ReturnsHeaderOnly()
        {
            var msg = NetSdrMessageHelper.GetDataItemMessage(
                NetSdrMessageHelper.MsgTypes.DataItem0,
                Array.Empty<byte>());

            Assert.That(msg, Has.Length.EqualTo(2)); // тільки header
        }

        [Test]
        public void GetDataItemMessage_TypeEncodedCorrectly()
        {
            var type = NetSdrMessageHelper.MsgTypes.DataItem0;
            var msg = NetSdrMessageHelper.GetDataItemMessage(type, new byte[100]);

            var num = BitConverter.ToUInt16(msg.Take(2).ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);

            Assert.That(actualType, Is.EqualTo(type));
        }

        [Test]
        public void TranslateMessage_ControlItem_ReturnsCorrectTypeAndCode()
        {
            // Arrange — побудувати повідомлення і одразу розібрати
            var type = NetSdrMessageHelper.MsgTypes.SetControlItem;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverFrequency;
            var parameters = new byte[] { 0x01, 0x02, 0x03 };
            var msg = NetSdrMessageHelper.GetControlItemMessage(type, code, parameters);

            // Act
            var success = NetSdrMessageHelper.TranslateMessage(msg, out var outType, out var outCode, out var outSeq, out var body);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(success, Is.True);
                Assert.That(outType, Is.EqualTo(type));
                Assert.That(outCode, Is.EqualTo(code));
                Assert.That(outSeq, Is.EqualTo(0));
                Assert.That(body, Is.EqualTo(parameters));
            });
        }

        [Test]
        public void TranslateMessage_DataItem_ReturnsCorrectTypeAndSequence()
        {
            // Arrange
            var type = NetSdrMessageHelper.MsgTypes.DataItem0;
            var parameters = new byte[] { 0x10, 0x20, 0x30, 0x40 };
            var msg = NetSdrMessageHelper.GetDataItemMessage(type, parameters);

            // Act
            var success = NetSdrMessageHelper.TranslateMessage(msg, out var outType, out _, out var outSeq, out var body);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(success, Is.True);
                Assert.That(outType, Is.EqualTo(type));
                Assert.That(body, Has.Length.GreaterThan(0));
            });
        }

        [Test]
        public void TranslateMessage_AckType_ParsedCorrectly()
        {
            var type = NetSdrMessageHelper.MsgTypes.Ack;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverState;
            var msg = NetSdrMessageHelper.GetControlItemMessage(type, code, new byte[2]);

            var success = NetSdrMessageHelper.TranslateMessage(msg, out var outType, out var outCode, out _, out _);

            Assert.Multiple(() =>
            {
                Assert.That(success, Is.True);
                Assert.That(outType, Is.EqualTo(type));
                Assert.That(outCode, Is.EqualTo(code));
            });
        }

        [Test]
        public void GetSamples_16bit_ReturnsCorrectCount()
        {
            // Arrange — 8 байт = 4 семпли по 16 біт
            var body = new byte[] { 0x01, 0x00, 0x02, 0x00, 0x03, 0x00, 0x04, 0x00 };

            // Act
            var samples = NetSdrMessageHelper.GetSamples(16, body).ToList();

            // Assert
            Assert.That(samples, Has.Count.EqualTo(4));
        }

        [Test]
        public void GetSamples_16bit_ReturnsCorrectValues()
        {
            var body = new byte[] { 0x05, 0x00, 0x0A, 0x00 };

            var samples = NetSdrMessageHelper.GetSamples(16, body).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(samples[0], Is.EqualTo(5));
                Assert.That(samples[1], Is.EqualTo(10));
            });
        }

        [Test]
        public void GetSamples_8bit_ReturnsCorrectCount()
        {
            var body = new byte[] { 0x01, 0x02, 0x03, 0x04 };

            var samples = NetSdrMessageHelper.GetSamples(8, body).ToList();

            Assert.That(samples, Has.Count.EqualTo(4));
        }

        [Test]
        public void GetSamples_EmptyBody_ReturnsNoSamples()
        {
            var samples = NetSdrMessageHelper.GetSamples(16, Array.Empty<byte>()).ToList();

            Assert.That(samples, Has.Count.EqualTo(0));
        }

        [Test]
        public void GetSamples_InvalidSampleSize_ThrowsException()
        {
            // sampleSize > 32 біти (4 байти) — має кинути виняток
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                NetSdrMessageHelper.GetSamples(64, new byte[] { 0x01 }).ToList());
        }

        [Test]
        public void GetHeader_MessageTooLong_ThrowsArgumentException()
        {
            // параметри довжиною більше ніж maxMessageLength
            var hugeParams = new byte[9000];

            Assert.Throws<ArgumentException>(() =>
                NetSdrMessageHelper.GetControlItemMessage(
                    NetSdrMessageHelper.MsgTypes.SetControlItem,
                    NetSdrMessageHelper.ControlItemCodes.ReceiverState,
                    hugeParams));
        }

        [Test]
        public void GetControlItemMessage_ZeroParams_LengthIsHeaderPlusCode()
        {
            var msg = NetSdrMessageHelper.GetControlItemMessage(
                NetSdrMessageHelper.MsgTypes.CurrentControlItem,
                NetSdrMessageHelper.ControlItemCodes.ADModes,
                Array.Empty<byte>());

            // 2 байти header + 2 байти code = 4
            Assert.That(msg, Has.Length.EqualTo(4));
        }

        [Test]
        public void GetDataItemMessage_NoneCode_NoCodeBytesInMessage()
        {
            var parameters = new byte[] { 0xAA, 0xBB };
            var msg = NetSdrMessageHelper.GetDataItemMessage(
                NetSdrMessageHelper.MsgTypes.DataItem1,
                parameters);

            // 2 байти header + 2 байти parameters = 4 (без code)
            Assert.That(msg, Has.Length.EqualTo(4));
        }

    // Lab8: coverage for IterateSamples (split from GetSamples)
    [Test]
    public void GetSamples_16bit_OddBytes_IgnoresTrailingByte()
    {
        // 5 байт при 16-bit = 2 семпли, 1 байт ігнорується
        var body = new byte[] { 0x01, 0x00, 0x02, 0x00, 0xFF };
        var samples = NetSdrMessageHelper.GetSamples(16, body).ToList();
        Assert.That(samples, Has.Count.EqualTo(2));
    }

    [Test]
    public void GetSamples_24bit_ReturnsCorrectCount()
    {
        // 6 байт при 24-bit = 2 семпли
        var body = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 };
        var samples = NetSdrMessageHelper.GetSamples(24, body).ToList();
        Assert.That(samples, Has.Count.EqualTo(2));
    }

    [Test]
    public void GetSamples_32bit_MultipleValues()
    {
        // 8 байт при 32-bit = 2 семпли
        var body = new byte[] { 0x01, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00 };
        var samples = NetSdrMessageHelper.GetSamples(32, body).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(samples, Has.Count.EqualTo(2));
            Assert.That(samples[0], Is.EqualTo(1));
            Assert.That(samples[1], Is.EqualTo(2));
        });
    }

        [Test]
        public void GetHeader_DataItem_MaxLength_EdgeCase()
        {
            // DataItem з параметрами що дають рівно _maxDataItemMessageLength (8194)
            // 8194 - 2 (header) = 8192 bytes params
            var msg = NetSdrMessageHelper.GetDataItemMessage(
                NetSdrMessageHelper.MsgTypes.DataItem0,
                new byte[8192]);
            // lengthWithHeader == 8194 → встановлюється 0
            var headerVal = BitConverter.ToUInt16(msg.Take(2).ToArray());
            var msgType = (NetSdrMessageHelper.MsgTypes)(headerVal >> 13);
            Assert.That(msgType, Is.EqualTo(NetSdrMessageHelper.MsgTypes.DataItem0));
        }

        [Test]
        public void TranslateMessage_DataItem_MaxLength_EdgeCase()
        {
            // Перевірити що TranslateHeader правильно обробляє msgLength == 0
            var msg = NetSdrMessageHelper.GetDataItemMessage(
                NetSdrMessageHelper.MsgTypes.DataItem0,
                new byte[8192]);
            var success = NetSdrMessageHelper.TranslateMessage(
                msg, out var outType, out _, out _, out var body);
            Assert.Multiple(() =>
            {
                Assert.That(success, Is.True);
                Assert.That(outType, Is.EqualTo(NetSdrMessageHelper.MsgTypes.DataItem0));
                Assert.That(body, Has.Length.EqualTo(8190)); // 8192 - 2 sequence number bytes
            });
        }

        [Test]
        public void GetSamples_ReturnsCorrectType()
        {
            // Перевірити що GetSamples повертає IEnumerable
            var result = NetSdrMessageHelper.GetSamples(16, new byte[] { 0x01, 0x00 });
            Assert.That(result, Is.Not.Null);
        }

    }
}
