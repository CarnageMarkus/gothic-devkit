using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.IO;

namespace Peter.CSParser
{
    public class Token
    {
        public int kind;    // token kind
        public int pos;     // token position in the source text (starting at 0)
        public int col;     // token column (starting at 1)
        public int line;    // token line (starting at 1)
        public string val;  // token value
        public Token next;  // ML 2005-03-11 Tokens are kept in linked list
    }

    //-----------------------------------------------------------------------------------
    // Buffer
    //-----------------------------------------------------------------------------------
    public class Buffer
    {
        // This Buffer supports the following cases:
        // 1) seekable stream (file)
        //    a) whole stream in buffer
        //    b) part of stream in buffer
        // 2) non seekable stream (network, console)

        public const int EOF = char.MaxValue + 1;
        const int MIN_BUFFER_LENGTH = 1024; // 1KB
        const int MAX_BUFFER_LENGTH = MIN_BUFFER_LENGTH * 64; // 64KB
        byte[] buf;         // input buffer
        int bufStart;       // position of first byte in buffer relative to input stream
        int bufLen;         // length of buffer
        int fileLen;        // length of input stream (may change if the stream is no file)
        int bufPos;         // current position in buffer
        Stream stream;      // input stream (seekable)
        bool isUserStream;  // was the stream opened by the user?

        public Buffer(Stream s, bool isUserStream)
        {
            stream = s; this.isUserStream = isUserStream;

            if (stream.CanSeek)
            {
                fileLen = (int)stream.Length;
                bufLen = Math.Min(fileLen, MAX_BUFFER_LENGTH);
                bufStart = Int32.MaxValue; // nothing in the buffer so far
            }
            else
            {
                fileLen = bufLen = bufStart = 0;
            }

            buf = new byte[(bufLen > 0) ? bufLen : MIN_BUFFER_LENGTH];
            if (fileLen > 0) Pos = 0; // setup buffer to position 0 (start)
            else bufPos = 0; // index 0 is already after the file, thus Pos = 0 is invalid
            if (bufLen == fileLen && stream.CanSeek) Close();
        }

        protected Buffer(Buffer b)
        { // called in UTF8Buffer constructor
            buf = b.buf;
            bufStart = b.bufStart;
            bufLen = b.bufLen;
            fileLen = b.fileLen;
            bufPos = b.bufPos;
            stream = b.stream;
            // keep destructor from closing the stream
            b.stream = null;
            isUserStream = b.isUserStream;
        }

        ~Buffer() { Close(); }

        protected void Close()
        {
            if (!isUserStream && stream != null)
            {
                stream.Close();
                stream = null;
            }
        }

        public virtual int Read()
        {
            if (bufPos < bufLen)
            {
                return buf[bufPos++];
            }
            else if (Pos < fileLen)
            {
                Pos = Pos; // shift buffer start to Pos
                return buf[bufPos++];
            }
            else if (stream != null && !stream.CanSeek && ReadNextStreamChunk() > 0)
            {
                return buf[bufPos++];
            }
            else
            {
                return EOF;
            }
        }

        public int Peek()
        {
            int curPos = Pos;
            int ch = Read();
            Pos = curPos;
            return ch;
        }

        public string GetString(int beg, int end)
        {
            int len = end - beg;
            char[] buf = new char[len];
            int oldPos = Pos;
            Pos = beg;
            for (int i = 0; i < len; i++) buf[i] = (char)Read();
            Pos = oldPos;
            return new String(buf);
        }

        public int Pos
        {
            get { return bufPos + bufStart; }
            set
            {
                if (value >= fileLen && stream != null && !stream.CanSeek)
                {
                    // Wanted position is after buffer and the stream
                    // is not seek-able e.g. network or console,
                    // thus we have to read the stream manually till
                    // the wanted position is in sight.
                    while (value >= fileLen && ReadNextStreamChunk() > 0) ;
                }

                if (value < 0 || value > fileLen)
                {
                    throw new FatalError("buffer out of bounds access, position: " + value);
                }

                if (value >= bufStart && value < bufStart + bufLen)
                { // already in buffer
                    bufPos = value - bufStart;
                }
                else if (stream != null)
                { // must be swapped in
                    stream.Seek(value, SeekOrigin.Begin);
                    bufLen = stream.Read(buf, 0, buf.Length);
                    bufStart = value; bufPos = 0;
                }
                else
                {
                    // set the position to the end of the file, Pos will return fileLen.
                    bufPos = fileLen - bufStart;
                }
            }
        }

        // Read the next chunk of bytes from the stream, increases the buffer
        // if needed and updates the fields fileLen and bufLen.
        // Returns the number of bytes read.
        private int ReadNextStreamChunk()
        {
            int free = buf.Length - bufLen;
            if (free == 0)
            {
                // in the case of a growing input stream
                // we can neither seek in the stream, nor can we
                // foresee the maximum length, thus we must adapt
                // the buffer size on demand.
                byte[] newBuf = new byte[bufLen * 2];
                Array.Copy(buf, newBuf, bufLen);
                buf = newBuf;
                free = bufLen;
            }
            int read = stream.Read(buf, bufLen, free);
            if (read > 0)
            {
                fileLen = bufLen = (bufLen + read);
                return read;
            }
            // end of stream reached
            return 0;
        }
    }

    //-----------------------------------------------------------------------------------
    // UTF8Buffer
    //-----------------------------------------------------------------------------------
    public class UTF8Buffer : Buffer
    {
        public UTF8Buffer(Buffer b) : base(b) { }

        public override int Read()
        {
            int ch;
            do
            {
                ch = base.Read();
                // until we find a uft8 start (0xxxxxxx or 11xxxxxx)
            } while ((ch >= 128) && ((ch & 0xC0) != 0xC0) && (ch != EOF));
            if (ch < 128 || ch == EOF)
            {
                // nothing to do, first 127 chars are the same in ascii and utf8
                // 0xxxxxxx or end of file character
            }
            else if ((ch & 0xF0) == 0xF0)
            {
                // 11110xxx 10xxxxxx 10xxxxxx 10xxxxxx
                int c1 = ch & 0x07; ch = base.Read();
                int c2 = ch & 0x3F; ch = base.Read();
                int c3 = ch & 0x3F; ch = base.Read();
                int c4 = ch & 0x3F;
                ch = (((((c1 << 6) | c2) << 6) | c3) << 6) | c4;
            }
            else if ((ch & 0xE0) == 0xE0)
            {
                // 1110xxxx 10xxxxxx 10xxxxxx
                int c1 = ch & 0x0F; ch = base.Read();
                int c2 = ch & 0x3F; ch = base.Read();
                int c3 = ch & 0x3F;
                ch = (((c1 << 6) | c2) << 6) | c3;
            }
            else if ((ch & 0xC0) == 0xC0)
            {
                // 110xxxxx 10xxxxxx
                int c1 = ch & 0x1F; ch = base.Read();
                int c2 = ch & 0x3F;
                ch = (c1 << 6) | c2;
            }
            return ch;
        }
    }

    //-----------------------------------------------------------------------------------
    // Scanner
    //-----------------------------------------------------------------------------------
    public class Scanner
    {
        const char EOL = '\n';
        const int eofSym = 0; /* pdt */
        const int maxT = 130;
        const int noSym = 130;


        public Buffer buffer; // scanner buffer

        Token t;          // current token
        int ch;           // current input character
        int pos;          // byte position of current character
        int col;          // column number of current character
        int line;         // line number of current character
        int oldEols;      // EOLs that appeared in a comment;
        Dictionary<int, int> start; // maps first token character to start state

        Token tokens;     // list of tokens already peeked (first token is a dummy)
        Token pt;         // current peek token

        char[] tval = new char[128]; // text of current token
        int tlen;         // length of current token

        public Scanner(string fileName)
        {
            try
            {
                Stream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                buffer = new Buffer(stream, false);
                Init();
            }
            catch (IOException)
            {
                throw new FatalError("Cannot open file " + fileName);
            }
        }

        public Scanner(Stream s)
        {
            buffer = new Buffer(s, true);
            Init();
        }

        void Init()
        {
            pos = -1; line = 1; col = 0;
            oldEols = 0;
            NextCh();
            if (ch == 0xEF)
            { // check optional byte order mark for UTF-8
                NextCh(); int ch1 = ch;
                NextCh(); int ch2 = ch;
                if (ch1 != 0xBB || ch2 != 0xBF)
                {
                    throw new FatalError(String.Format("illegal byte order mark: EF {0,2:X} {1,2:X}", ch1, ch2));
                }
                buffer = new UTF8Buffer(buffer); col = 0;
                NextCh();
            }
            start = new Dictionary<int, int>(128);
            for (int i = 65; i <= 90; ++i) start[i] = 1;
            for (int i = 95; i <= 95; ++i) start[i] = 1;
            for (int i = 97; i <= 122; ++i) start[i] = 1;
            for (int i = 170; i <= 170; ++i) start[i] = 1;
            for (int i = 181; i <= 181; ++i) start[i] = 1;
            for (int i = 186; i <= 186; ++i) start[i] = 1;
            for (int i = 192; i <= 214; ++i) start[i] = 1;
            for (int i = 216; i <= 246; ++i) start[i] = 1;
            for (int i = 248; i <= 255; ++i) start[i] = 1;
            for (int i = 49; i <= 57; ++i) start[i] = 159;
            start[92] = 15;
            start[64] = 160;
            start[48] = 161;
            start[46] = 162;
            start[39] = 44;
            start[34] = 61;
            start[38] = 196;
            start[61] = 163;
            start[58] = 164;
            start[44] = 79;
            start[45] = 197;
            start[47] = 198;
            start[62] = 165;
            start[43] = 166;
            start[123] = 86;
            start[91] = 87;
            start[40] = 88;
            start[60] = 199;
            start[37] = 200;
            start[33] = 167;
            start[124] = 201;
            start[63] = 202;
            start[125] = 95;
            start[93] = 96;
            start[41] = 97;
            start[59] = 98;
            start[126] = 99;
            start[42] = 168;
            start[94] = 203;
            start[35] = 169;
            start[Buffer.EOF] = -1;

            pt = tokens = new Token();  // first token is a dummy
        }

        void NextCh()
        {
            if (oldEols > 0) { ch = EOL; oldEols--; }
            else
            {
                pos = buffer.Pos;
                ch = buffer.Read(); col++;
                // replace isolated '\r' by '\n' in order to make
                // eol handling uniform across Windows, Unix and Mac
                if (ch == '\r' && buffer.Peek() != '\n') ch = EOL;
                if (ch == EOL) { line++; col = 0; }
            }

        }

        void AddCh()
        {
            if (tlen >= tval.Length)
            {
                char[] newBuf = new char[2 * tval.Length];
                Array.Copy(tval, 0, newBuf, 0, tval.Length);
                tval = newBuf;
            }
            tval[tlen++] = (char)ch;
            NextCh();
        }



        bool Comment0()
        {
            int level = 1, pos0 = pos, line0 = line, col0 = col;
            NextCh();
            if (ch == '/')
            {
                NextCh();
                for (; ; )
                {
                    if (ch == 10)
                    {
                        level--;
                        if (level == 0) { oldEols = line - line0; NextCh(); return true; }
                        NextCh();
                    }
                    else if (ch == Buffer.EOF) return false;
                    else NextCh();
                }
            }
            else
            {
                buffer.Pos = pos0; NextCh(); line = line0; col = col0;
            }
            return false;
        }

        bool Comment1()
        {
            int level = 1, pos0 = pos, line0 = line, col0 = col;
            NextCh();
            if (ch == '*')
            {
                NextCh();
                for (; ; )
                {
                    if (ch == '*')
                    {
                        NextCh();
                        if (ch == '/')
                        {
                            level--;
                            if (level == 0) { oldEols = line - line0; NextCh(); return true; }
                            NextCh();
                        }
                    }
                    else if (ch == Buffer.EOF) return false;
                    else NextCh();
                }
            }
            else
            {
                buffer.Pos = pos0; NextCh(); line = line0; col = col0;
            }
            return false;
        }


        void CheckLiteral()
        {
            switch (t.val)
            {
                case "abstract": t.kind = 6; break;
                case "as": t.kind = 7; break;
                case "base": t.kind = 8; break;
                case "bool": t.kind = 9; break;
                case "break": t.kind = 10; break;
                case "byte": t.kind = 11; break;
                case "case": t.kind = 12; break;
                case "catch": t.kind = 13; break;
                case "char": t.kind = 14; break;
                case "checked": t.kind = 15; break;
                case "class": t.kind = 16; break;
                case "const": t.kind = 17; break;
                case "continue": t.kind = 18; break;
                case "decimal": t.kind = 19; break;
                case "default": t.kind = 20; break;
                case "delegate": t.kind = 21; break;
                case "do": t.kind = 22; break;
                case "double": t.kind = 23; break;
                case "else": t.kind = 24; break;
                case "enum": t.kind = 25; break;
                case "event": t.kind = 26; break;
                case "explicit": t.kind = 27; break;
                case "extern": t.kind = 28; break;
                case "false": t.kind = 29; break;
                case "finally": t.kind = 30; break;
                case "fixed": t.kind = 31; break;
                case "float": t.kind = 32; break;
                case "for": t.kind = 33; break;
                case "foreach": t.kind = 34; break;
                case "goto": t.kind = 35; break;
                case "if": t.kind = 36; break;
                case "implicit": t.kind = 37; break;
                case "in": t.kind = 38; break;
                case "int": t.kind = 39; break;
                case "interface": t.kind = 40; break;
                case "internal": t.kind = 41; break;
                case "is": t.kind = 42; break;
                case "lock": t.kind = 43; break;
                case "long": t.kind = 44; break;
                case "namespace": t.kind = 45; break;
                case "new": t.kind = 46; break;
                case "null": t.kind = 47; break;
                case "object": t.kind = 48; break;
                case "operator": t.kind = 49; break;
                case "out": t.kind = 50; break;
                case "override": t.kind = 51; break;
                case "params": t.kind = 52; break;
                case "private": t.kind = 53; break;
                case "protected": t.kind = 54; break;
                case "public": t.kind = 55; break;
                case "readonly": t.kind = 56; break;
                case "ref": t.kind = 57; break;
                case "return": t.kind = 58; break;
                case "sbyte": t.kind = 59; break;
                case "sealed": t.kind = 60; break;
                case "short": t.kind = 61; break;
                case "sizeof": t.kind = 62; break;
                case "stackalloc": t.kind = 63; break;
                case "static": t.kind = 64; break;
                case "string": t.kind = 65; break;
                case "struct": t.kind = 66; break;
                case "switch": t.kind = 67; break;
                case "this": t.kind = 68; break;
                case "throw": t.kind = 69; break;
                case "true": t.kind = 70; break;
                case "try": t.kind = 71; break;
                case "typeof": t.kind = 72; break;
                case "uint": t.kind = 73; break;
                case "ulong": t.kind = 74; break;
                case "unchecked": t.kind = 75; break;
                case "unsafe": t.kind = 76; break;
                case "ushort": t.kind = 77; break;
                case "using": t.kind = 78; break;
                case "virtual": t.kind = 79; break;
                case "void": t.kind = 80; break;
                case "volatile": t.kind = 81; break;
                case "while": t.kind = 82; break;
                case "partial": t.kind = 119; break;
                case "yield": t.kind = 120; break;
                default: break;
            }
        }

        Token NextToken()
        {
            while (ch == ' ' ||
                ch >= 9 && ch <= 10 || ch == 13
            ) NextCh();
            if (ch == '/' && Comment0() || ch == '/' && Comment1()) return NextToken();
            int apx = 0;
            t = new Token();
            t.pos = pos; t.col = col; t.line = line;
            int state;
            try { state = start[ch]; }
            catch (KeyNotFoundException) { state = 0; }
            tlen = 0; AddCh();

            switch (state)
            {
                case -1: { t.kind = eofSym; break; } // NextCh already done
                case 0: { t.kind = noSym; break; }   // NextCh already done
                case 1:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'Z' || ch == '_' || ch >= 'a' && ch <= 'z' || ch == 160 || ch == 170 || ch == 181 || ch == 186 || ch >= 192 && ch <= 214 || ch >= 216 && ch <= 246 || ch >= 248 && ch <= 255) { AddCh(); goto case 1; }
                    else if (ch == 92) { AddCh(); goto case 2; }
                    else { t.kind = 1; t.val = new String(tval, 0, tlen); CheckLiteral(); return t; }
                case 2:
                    if (ch == 'u') { AddCh(); goto case 3; }
                    else if (ch == 'U') { AddCh(); goto case 7; }
                    else { t.kind = noSym; break; }
                case 3:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 4; }
                    else { t.kind = noSym; break; }
                case 4:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 5; }
                    else { t.kind = noSym; break; }
                case 5:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 6; }
                    else { t.kind = noSym; break; }
                case 6:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 1; }
                    else { t.kind = noSym; break; }
                case 7:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 8; }
                    else { t.kind = noSym; break; }
                case 8:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 9; }
                    else { t.kind = noSym; break; }
                case 9:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 10; }
                    else { t.kind = noSym; break; }
                case 10:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 11; }
                    else { t.kind = noSym; break; }
                case 11:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 12; }
                    else { t.kind = noSym; break; }
                case 12:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 13; }
                    else { t.kind = noSym; break; }
                case 13:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 14; }
                    else { t.kind = noSym; break; }
                case 14:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 1; }
                    else { t.kind = noSym; break; }
                case 15:
                    if (ch == 'u') { AddCh(); goto case 16; }
                    else if (ch == 'U') { AddCh(); goto case 20; }
                    else { t.kind = noSym; break; }
                case 16:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 17; }
                    else { t.kind = noSym; break; }
                case 17:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 18; }
                    else { t.kind = noSym; break; }
                case 18:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 19; }
                    else { t.kind = noSym; break; }
                case 19:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 1; }
                    else { t.kind = noSym; break; }
                case 20:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 21; }
                    else { t.kind = noSym; break; }
                case 21:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 22; }
                    else { t.kind = noSym; break; }
                case 22:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 23; }
                    else { t.kind = noSym; break; }
                case 23:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 24; }
                    else { t.kind = noSym; break; }
                case 24:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 25; }
                    else { t.kind = noSym; break; }
                case 25:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 26; }
                    else { t.kind = noSym; break; }
                case 26:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 27; }
                    else { t.kind = noSym; break; }
                case 27:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 1; }
                    else { t.kind = noSym; break; }
                case 28:
                    if (ch >= '0' && ch <= '9') { apx = 0; AddCh(); goto case 28; }
                    else if (ch == 'U') { apx = 0; AddCh(); goto case 170; }
                    else if (ch == 'u') { apx = 0; AddCh(); goto case 171; }
                    else if (ch == 'L') { apx = 0; AddCh(); goto case 172; }
                    else if (ch == 'l') { apx = 0; AddCh(); goto case 173; }
                    else
                    {
                        tlen -= apx;
                        buffer.Pos = t.pos; NextCh(); line = t.line; col = t.col;
                        for (int i = 0; i < tlen; i++) NextCh();
                        t.kind = 2; break;
                    }
                case 29:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 30; }
                    else { t.kind = noSym; break; }
                case 30:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 30; }
                    else if (ch == 'U') { AddCh(); goto case 174; }
                    else if (ch == 'u') { AddCh(); goto case 175; }
                    else if (ch == 'L') { AddCh(); goto case 176; }
                    else if (ch == 'l') { AddCh(); goto case 177; }
                    else { t.kind = 2; break; }
                case 31:
                    { t.kind = 2; break; }
                case 32:
                    if (ch >= '0' && ch <= '9') { AddCh(); goto case 32; }
                    else if (ch == 'D' || ch == 'F' || ch == 'M' || ch == 'd' || ch == 'f' || ch == 'm') { AddCh(); goto case 43; }
                    else if (ch == 'E' || ch == 'e') { AddCh(); goto case 33; }
                    else { t.kind = 3; break; }
                case 33:
                    if (ch >= '0' && ch <= '9') { AddCh(); goto case 35; }
                    else if (ch == '+' || ch == '-') { AddCh(); goto case 34; }
                    else { t.kind = noSym; break; }
                case 34:
                    if (ch >= '0' && ch <= '9') { AddCh(); goto case 35; }
                    else { t.kind = noSym; break; }
                case 35:
                    if (ch >= '0' && ch <= '9') { AddCh(); goto case 35; }
                    else if (ch == 'D' || ch == 'F' || ch == 'M' || ch == 'd' || ch == 'f' || ch == 'm') { AddCh(); goto case 43; }
                    else { t.kind = 3; break; }
                case 36:
                    if (ch >= '0' && ch <= '9') { AddCh(); goto case 36; }
                    else if (ch == 'D' || ch == 'F' || ch == 'M' || ch == 'd' || ch == 'f' || ch == 'm') { AddCh(); goto case 43; }
                    else if (ch == 'E' || ch == 'e') { AddCh(); goto case 37; }
                    else { t.kind = 3; break; }
                case 37:
                    if (ch >= '0' && ch <= '9') { AddCh(); goto case 39; }
                    else if (ch == '+' || ch == '-') { AddCh(); goto case 38; }
                    else { t.kind = noSym; break; }
                case 38:
                    if (ch >= '0' && ch <= '9') { AddCh(); goto case 39; }
                    else { t.kind = noSym; break; }
                case 39:
                    if (ch >= '0' && ch <= '9') { AddCh(); goto case 39; }
                    else if (ch == 'D' || ch == 'F' || ch == 'M' || ch == 'd' || ch == 'f' || ch == 'm') { AddCh(); goto case 43; }
                    else { t.kind = 3; break; }
                case 40:
                    if (ch >= '0' && ch <= '9') { AddCh(); goto case 42; }
                    else if (ch == '+' || ch == '-') { AddCh(); goto case 41; }
                    else { t.kind = noSym; break; }
                case 41:
                    if (ch >= '0' && ch <= '9') { AddCh(); goto case 42; }
                    else { t.kind = noSym; break; }
                case 42:
                    if (ch >= '0' && ch <= '9') { AddCh(); goto case 42; }
                    else if (ch == 'D' || ch == 'F' || ch == 'M' || ch == 'd' || ch == 'f' || ch == 'm') { AddCh(); goto case 43; }
                    else { t.kind = 3; break; }
                case 43:
                    { t.kind = 3; break; }
                case 44:
                    if (ch <= 9 || ch >= 11 && ch <= 12 || ch >= 14 && ch <= '&' || ch >= '(' && ch <= '[' || ch >= ']' && ch <= 65535) { AddCh(); goto case 45; }
                    else if (ch == 92) { AddCh(); goto case 178; }
                    else { t.kind = noSym; break; }
                case 45:
                    if (ch == 39) { AddCh(); goto case 60; }
                    else { t.kind = noSym; break; }
                case 46:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 47; }
                    else { t.kind = noSym; break; }
                case 47:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 179; }
                    else if (ch == 39) { AddCh(); goto case 60; }
                    else { t.kind = noSym; break; }
                case 48:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 49; }
                    else { t.kind = noSym; break; }
                case 49:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 50; }
                    else { t.kind = noSym; break; }
                case 50:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 51; }
                    else { t.kind = noSym; break; }
                case 51:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 45; }
                    else { t.kind = noSym; break; }
                case 52:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 53; }
                    else { t.kind = noSym; break; }
                case 53:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 54; }
                    else { t.kind = noSym; break; }
                case 54:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 55; }
                    else { t.kind = noSym; break; }
                case 55:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 56; }
                    else { t.kind = noSym; break; }
                case 56:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 57; }
                    else { t.kind = noSym; break; }
                case 57:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 58; }
                    else { t.kind = noSym; break; }
                case 58:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 59; }
                    else { t.kind = noSym; break; }
                case 59:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 45; }
                    else { t.kind = noSym; break; }
                case 60:
                    { t.kind = 4; break; }
                case 61:
                    if (ch <= 9 || ch >= 11 && ch <= 12 || ch >= 14 && ch <= '!' || ch >= '#' && ch <= '[' || ch >= ']' && ch <= 65535) { AddCh(); goto case 61; }
                    else if (ch == '"') { AddCh(); goto case 77; }
                    else if (ch == 92) { AddCh(); goto case 181; }
                    else { t.kind = noSym; break; }
                case 62:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 63; }
                    else { t.kind = noSym; break; }
                case 63:
                    if (ch <= 9 || ch >= 11 && ch <= 12 || ch >= 14 && ch <= '!' || ch >= '#' && ch <= '/' || ch >= ':' && ch <= '@' || ch >= 'G' && ch <= '[' || ch >= ']' && ch <= '`' || ch >= 'g' && ch <= 65535) { AddCh(); goto case 61; }
                    else if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 182; }
                    else if (ch == '"') { AddCh(); goto case 77; }
                    else if (ch == 92) { AddCh(); goto case 181; }
                    else { t.kind = noSym; break; }
                case 64:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 65; }
                    else { t.kind = noSym; break; }
                case 65:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 66; }
                    else { t.kind = noSym; break; }
                case 66:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 67; }
                    else { t.kind = noSym; break; }
                case 67:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 61; }
                    else { t.kind = noSym; break; }
                case 68:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 69; }
                    else { t.kind = noSym; break; }
                case 69:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 70; }
                    else { t.kind = noSym; break; }
                case 70:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 71; }
                    else { t.kind = noSym; break; }
                case 71:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 72; }
                    else { t.kind = noSym; break; }
                case 72:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 73; }
                    else { t.kind = noSym; break; }
                case 73:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 74; }
                    else { t.kind = noSym; break; }
                case 74:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 75; }
                    else { t.kind = noSym; break; }
                case 75:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 61; }
                    else { t.kind = noSym; break; }
                case 76:
                    if (ch <= '!' || ch >= '#' && ch <= 65535) { AddCh(); goto case 76; }
                    else if (ch == '"') { AddCh(); goto case 184; }
                    else { t.kind = noSym; break; }
                case 77:
                    { t.kind = 5; break; }
                case 78:
                    { t.kind = 84; break; }
                case 79:
                    { t.kind = 87; break; }
                case 80:
                    { t.kind = 88; break; }
                case 81:
                    { t.kind = 89; break; }
                case 82:
                    { t.kind = 91; break; }
                case 83:
                    { t.kind = 92; break; }
                case 84:
                    { t.kind = 94; break; }
                case 85:
                    { t.kind = 95; break; }
                case 86:
                    { t.kind = 96; break; }
                case 87:
                    { t.kind = 97; break; }
                case 88:
                    { t.kind = 98; break; }
                case 89:
                    { t.kind = 99; break; }
                case 90:
                    { t.kind = 103; break; }
                case 91:
                    { t.kind = 104; break; }
                case 92:
                    { t.kind = 105; break; }
                case 93:
                    { t.kind = 107; break; }
                case 94:
                    { t.kind = 109; break; }
                case 95:
                    { t.kind = 111; break; }
                case 96:
                    { t.kind = 112; break; }
                case 97:
                    { t.kind = 113; break; }
                case 98:
                    { t.kind = 114; break; }
                case 99:
                    { t.kind = 115; break; }
                case 100:
                    { t.kind = 117; break; }
                case 101:
                    { t.kind = 118; break; }
                case 102:
                    if (ch == 'e') { AddCh(); goto case 103; }
                    else { t.kind = noSym; break; }
                case 103:
                    if (ch == 'f') { AddCh(); goto case 104; }
                    else { t.kind = noSym; break; }
                case 104:
                    if (ch == 'i') { AddCh(); goto case 105; }
                    else { t.kind = noSym; break; }
                case 105:
                    if (ch == 'n') { AddCh(); goto case 106; }
                    else { t.kind = noSym; break; }
                case 106:
                    if (ch == 'e') { AddCh(); goto case 107; }
                    else { t.kind = noSym; break; }
                case 107:
                    if (ch == 10 || ch == 13) { AddCh(); goto case 108; }
                    else if (ch <= 9 || ch >= 11 && ch <= 12 || ch >= 14 && ch <= 65535) { AddCh(); goto case 107; }
                    else { t.kind = noSym; break; }
                case 108:
                    { t.kind = 131; break; }
                case 109:
                    if (ch == 'n') { AddCh(); goto case 110; }
                    else { t.kind = noSym; break; }
                case 110:
                    if (ch == 'd') { AddCh(); goto case 111; }
                    else { t.kind = noSym; break; }
                case 111:
                    if (ch == 'e') { AddCh(); goto case 112; }
                    else { t.kind = noSym; break; }
                case 112:
                    if (ch == 'f') { AddCh(); goto case 113; }
                    else { t.kind = noSym; break; }
                case 113:
                    if (ch == 10 || ch == 13) { AddCh(); goto case 114; }
                    else if (ch <= 9 || ch >= 11 && ch <= 12 || ch >= 14 && ch <= 65535) { AddCh(); goto case 113; }
                    else { t.kind = noSym; break; }
                case 114:
                    { t.kind = 132; break; }
                case 115:
                    if (ch == 'f') { AddCh(); goto case 116; }
                    else { t.kind = noSym; break; }
                case 116:
                    if (ch == 10 || ch == 13) { AddCh(); goto case 117; }
                    else if (ch <= 9 || ch >= 11 && ch <= 12 || ch >= 14 && ch <= 65535) { AddCh(); goto case 116; }
                    else { t.kind = noSym; break; }
                case 117:
                    { t.kind = 133; break; }
                case 118:
                    if (ch == 'f') { AddCh(); goto case 119; }
                    else { t.kind = noSym; break; }
                case 119:
                    if (ch == 10 || ch == 13) { AddCh(); goto case 120; }
                    else if (ch <= 9 || ch >= 11 && ch <= 12 || ch >= 14 && ch <= 65535) { AddCh(); goto case 119; }
                    else { t.kind = noSym; break; }
                case 120:
                    { t.kind = 134; break; }
                case 121:
                    if (ch == 'e') { AddCh(); goto case 122; }
                    else { t.kind = noSym; break; }
                case 122:
                    if (ch == 10 || ch == 13) { AddCh(); goto case 123; }
                    else if (ch <= 9 || ch >= 11 && ch <= 12 || ch >= 14 && ch <= 65535) { AddCh(); goto case 122; }
                    else { t.kind = noSym; break; }
                case 123:
                    { t.kind = 135; break; }
                case 124:
                    if (ch == 'f') { AddCh(); goto case 125; }
                    else { t.kind = noSym; break; }
                case 125:
                    if (ch == 10 || ch == 13) { AddCh(); goto case 126; }
                    else if (ch <= 9 || ch >= 11 && ch <= 12 || ch >= 14 && ch <= 65535) { AddCh(); goto case 125; }
                    else { t.kind = noSym; break; }
                case 126:
                    { t.kind = 136; break; }
                case 127:
                    if (ch == 'i') { AddCh(); goto case 128; }
                    else { t.kind = noSym; break; }
                case 128:
                    if (ch == 'n') { AddCh(); goto case 129; }
                    else { t.kind = noSym; break; }
                case 129:
                    if (ch == 'e') { AddCh(); goto case 130; }
                    else { t.kind = noSym; break; }
                case 130:
                    if (ch == 10 || ch == 13) { AddCh(); goto case 131; }
                    else if (ch <= 9 || ch >= 11 && ch <= 12 || ch >= 14 && ch <= 65535) { AddCh(); goto case 130; }
                    else { t.kind = noSym; break; }
                case 131:
                    { t.kind = 137; break; }
                case 132:
                    if (ch == 'r') { AddCh(); goto case 133; }
                    else { t.kind = noSym; break; }
                case 133:
                    if (ch == 'o') { AddCh(); goto case 134; }
                    else { t.kind = noSym; break; }
                case 134:
                    if (ch == 'r') { AddCh(); goto case 135; }
                    else { t.kind = noSym; break; }
                case 135:
                    if (ch == 10 || ch == 13) { AddCh(); goto case 136; }
                    else if (ch <= 9 || ch >= 11 && ch <= 12 || ch >= 14 && ch <= 65535) { AddCh(); goto case 135; }
                    else { t.kind = noSym; break; }
                case 136:
                    { t.kind = 138; break; }
                case 137:
                    if (ch == 'a') { AddCh(); goto case 138; }
                    else { t.kind = noSym; break; }
                case 138:
                    if (ch == 'r') { AddCh(); goto case 139; }
                    else { t.kind = noSym; break; }
                case 139:
                    if (ch == 'n') { AddCh(); goto case 140; }
                    else { t.kind = noSym; break; }
                case 140:
                    if (ch == 'i') { AddCh(); goto case 141; }
                    else { t.kind = noSym; break; }
                case 141:
                    if (ch == 'n') { AddCh(); goto case 142; }
                    else { t.kind = noSym; break; }
                case 142:
                    if (ch == 'g') { AddCh(); goto case 143; }
                    else { t.kind = noSym; break; }
                case 143:
                    if (ch == 10 || ch == 13) { AddCh(); goto case 144; }
                    else if (ch <= 9 || ch >= 11 && ch <= 12 || ch >= 14 && ch <= 65535) { AddCh(); goto case 143; }
                    else { t.kind = noSym; break; }
                case 144:
                    { t.kind = 139; break; }
                case 145:
                    if (ch == 'e') { AddCh(); goto case 146; }
                    else { t.kind = noSym; break; }
                case 146:
                    if (ch == 'g') { AddCh(); goto case 147; }
                    else { t.kind = noSym; break; }
                case 147:
                    if (ch == 'i') { AddCh(); goto case 148; }
                    else { t.kind = noSym; break; }
                case 148:
                    if (ch == 'o') { AddCh(); goto case 149; }
                    else { t.kind = noSym; break; }
                case 149:
                    if (ch == 'n') { AddCh(); goto case 150; }
                    else { t.kind = noSym; break; }
                case 150:
                    if (ch == 10 || ch == 13) { AddCh(); goto case 151; }
                    else if (ch <= 9 || ch >= 11 && ch <= 12 || ch >= 14 && ch <= 65535) { AddCh(); goto case 150; }
                    else { t.kind = noSym; break; }
                case 151:
                    { t.kind = 140; break; }
                case 152:
                    if (ch == 'e') { AddCh(); goto case 153; }
                    else { t.kind = noSym; break; }
                case 153:
                    if (ch == 'g') { AddCh(); goto case 154; }
                    else { t.kind = noSym; break; }
                case 154:
                    if (ch == 'i') { AddCh(); goto case 155; }
                    else { t.kind = noSym; break; }
                case 155:
                    if (ch == 'o') { AddCh(); goto case 156; }
                    else { t.kind = noSym; break; }
                case 156:
                    if (ch == 'n') { AddCh(); goto case 157; }
                    else { t.kind = noSym; break; }
                case 157:
                    if (ch == 10 || ch == 13) { AddCh(); goto case 158; }
                    else if (ch <= 9 || ch >= 11 && ch <= 12 || ch >= 14 && ch <= 65535) { AddCh(); goto case 157; }
                    else { t.kind = noSym; break; }
                case 158:
                    { t.kind = 141; break; }
                case 159:
                    if (ch >= '0' && ch <= '9') { apx = 0; AddCh(); goto case 159; }
                    else if (ch == 'U') { apx = 0; AddCh(); goto case 170; }
                    else if (ch == 'u') { apx = 0; AddCh(); goto case 171; }
                    else if (ch == 'L') { apx = 0; AddCh(); goto case 172; }
                    else if (ch == 'l') { apx = 0; AddCh(); goto case 173; }
                    else if (ch == '.') { apx++; AddCh(); goto case 185; }
                    else if (ch == 'E' || ch == 'e') { apx = 0; AddCh(); goto case 40; }
                    else if (ch == 'D' || ch == 'F' || ch == 'M' || ch == 'd' || ch == 'f' || ch == 'm') { apx = 0; AddCh(); goto case 43; }
                    else { t.kind = 2; break; }
                case 160:
                    if (ch >= 'A' && ch <= 'Z' || ch == '_' || ch >= 'a' && ch <= 'z' || ch == 170 || ch == 181 || ch == 186 || ch >= 192 && ch <= 214 || ch >= 216 && ch <= 246 || ch >= 248 && ch <= 255) { AddCh(); goto case 1; }
                    else if (ch == 92) { AddCh(); goto case 15; }
                    else if (ch == '"') { AddCh(); goto case 76; }
                    else { t.kind = noSym; break; }
                case 161:
                    if (ch >= '0' && ch <= '9') { apx = 0; AddCh(); goto case 159; }
                    else if (ch == 'U') { apx = 0; AddCh(); goto case 170; }
                    else if (ch == 'u') { apx = 0; AddCh(); goto case 171; }
                    else if (ch == 'L') { apx = 0; AddCh(); goto case 172; }
                    else if (ch == 'l') { apx = 0; AddCh(); goto case 173; }
                    else if (ch == '.') { apx++; AddCh(); goto case 185; }
                    else if (ch == 'X' || ch == 'x') { apx = 0; AddCh(); goto case 29; }
                    else if (ch == 'E' || ch == 'e') { apx = 0; AddCh(); goto case 40; }
                    else if (ch == 'D' || ch == 'F' || ch == 'M' || ch == 'd' || ch == 'f' || ch == 'm') { apx = 0; AddCh(); goto case 43; }
                    else { t.kind = 2; break; }
                case 162:
                    if (ch >= '0' && ch <= '9') { AddCh(); goto case 32; }
                    else { t.kind = 90; break; }
                case 163:
                    if (ch == '=') { AddCh(); goto case 83; }
                    else { t.kind = 85; break; }
                case 164:
                    if (ch == ':') { AddCh(); goto case 82; }
                    else { t.kind = 86; break; }
                case 165:
                    if (ch == '=') { AddCh(); goto case 84; }
                    else { t.kind = 93; break; }
                case 166:
                    if (ch == '+') { AddCh(); goto case 85; }
                    else if (ch == '=') { AddCh(); goto case 94; }
                    else { t.kind = 108; break; }
                case 167:
                    if (ch == '=') { AddCh(); goto case 92; }
                    else { t.kind = 106; break; }
                case 168:
                    if (ch == '=') { AddCh(); goto case 100; }
                    else { t.kind = 116; break; }
                case 169:
                    if (ch == 9 || ch >= 11 && ch <= 12 || ch == ' ') { AddCh(); goto case 169; }
                    else if (ch == 'd') { AddCh(); goto case 102; }
                    else if (ch == 'u') { AddCh(); goto case 109; }
                    else if (ch == 'i') { AddCh(); goto case 115; }
                    else if (ch == 'e') { AddCh(); goto case 187; }
                    else if (ch == 'l') { AddCh(); goto case 127; }
                    else if (ch == 'w') { AddCh(); goto case 137; }
                    else if (ch == 'r') { AddCh(); goto case 145; }
                    else { t.kind = noSym; break; }
                case 170:
                    if (ch == 'L' || ch == 'l') { AddCh(); goto case 31; }
                    else { t.kind = 2; break; }
                case 171:
                    if (ch == 'L' || ch == 'l') { AddCh(); goto case 31; }
                    else { t.kind = 2; break; }
                case 172:
                    if (ch == 'U' || ch == 'u') { AddCh(); goto case 31; }
                    else { t.kind = 2; break; }
                case 173:
                    if (ch == 'U' || ch == 'u') { AddCh(); goto case 31; }
                    else { t.kind = 2; break; }
                case 174:
                    if (ch == 'L' || ch == 'l') { AddCh(); goto case 31; }
                    else { t.kind = 2; break; }
                case 175:
                    if (ch == 'L' || ch == 'l') { AddCh(); goto case 31; }
                    else { t.kind = 2; break; }
                case 176:
                    if (ch == 'U' || ch == 'u') { AddCh(); goto case 31; }
                    else { t.kind = 2; break; }
                case 177:
                    if (ch == 'U' || ch == 'u') { AddCh(); goto case 31; }
                    else { t.kind = 2; break; }
                case 178:
                    if (ch == '"' || ch == 39 || ch == '0' || ch == 92 || ch >= 'a' && ch <= 'b' || ch == 'f' || ch == 'n' || ch == 'r' || ch == 't' || ch == 'v') { AddCh(); goto case 45; }
                    else if (ch == 'x') { AddCh(); goto case 46; }
                    else if (ch == 'u') { AddCh(); goto case 48; }
                    else if (ch == 'U') { AddCh(); goto case 52; }
                    else { t.kind = noSym; break; }
                case 179:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 180; }
                    else if (ch == 39) { AddCh(); goto case 60; }
                    else { t.kind = noSym; break; }
                case 180:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 45; }
                    else if (ch == 39) { AddCh(); goto case 60; }
                    else { t.kind = noSym; break; }
                case 181:
                    if (ch == '"' || ch == 39 || ch == '0' || ch == 92 || ch >= 'a' && ch <= 'b' || ch == 'f' || ch == 'n' || ch == 'r' || ch == 't' || ch == 'v') { AddCh(); goto case 61; }
                    else if (ch == 'x') { AddCh(); goto case 62; }
                    else if (ch == 'u') { AddCh(); goto case 64; }
                    else if (ch == 'U') { AddCh(); goto case 68; }
                    else { t.kind = noSym; break; }
                case 182:
                    if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 183; }
                    else if (ch <= 9 || ch >= 11 && ch <= 12 || ch >= 14 && ch <= '!' || ch >= '#' && ch <= '/' || ch >= ':' && ch <= '@' || ch >= 'G' && ch <= '[' || ch >= ']' && ch <= '`' || ch >= 'g' && ch <= 65535) { AddCh(); goto case 61; }
                    else if (ch == '"') { AddCh(); goto case 77; }
                    else if (ch == 92) { AddCh(); goto case 181; }
                    else { t.kind = noSym; break; }
                case 183:
                    if (ch <= 9 || ch >= 11 && ch <= 12 || ch >= 14 && ch <= '!' || ch >= '#' && ch <= '[' || ch >= ']' && ch <= 65535) { AddCh(); goto case 61; }
                    else if (ch == '"') { AddCh(); goto case 77; }
                    else if (ch == 92) { AddCh(); goto case 181; }
                    else { t.kind = noSym; break; }
                case 184:
                    if (ch == '"') { AddCh(); goto case 76; }
                    else { t.kind = 5; break; }
                case 185:
                    if (ch <= '/' || ch >= ':' && ch <= 65535) { apx++; AddCh(); goto case 28; }
                    else if (ch >= '0' && ch <= '9') { apx = 0; AddCh(); goto case 36; }
                    else { t.kind = noSym; break; }
                case 186:
                    if (ch == '=') { AddCh(); goto case 89; }
                    else { t.kind = 101; break; }
                case 187:
                    if (ch == 'l') { AddCh(); goto case 188; }
                    else if (ch == 'n') { AddCh(); goto case 189; }
                    else if (ch == 'r') { AddCh(); goto case 132; }
                    else { t.kind = noSym; break; }
                case 188:
                    if (ch == 'i') { AddCh(); goto case 118; }
                    else if (ch == 's') { AddCh(); goto case 121; }
                    else { t.kind = noSym; break; }
                case 189:
                    if (ch == 'd') { AddCh(); goto case 190; }
                    else { t.kind = noSym; break; }
                case 190:
                    if (ch == 'i') { AddCh(); goto case 124; }
                    else if (ch == 'r') { AddCh(); goto case 152; }
                    else { t.kind = noSym; break; }
                case 191:
                    { t.kind = 121; break; }
                case 192:
                    { t.kind = 122; break; }
                case 193:
                    { t.kind = 123; break; }
                case 194:
                    { t.kind = 126; break; }
                case 195:
                    { t.kind = 129; break; }
                case 196:
                    if (ch == '=') { AddCh(); goto case 78; }
                    else if (ch == '&') { AddCh(); goto case 193; }
                    else { t.kind = 83; break; }
                case 197:
                    if (ch == '-') { AddCh(); goto case 80; }
                    else if (ch == '=') { AddCh(); goto case 90; }
                    else if (ch == '>') { AddCh(); goto case 195; }
                    else { t.kind = 102; break; }
                case 198:
                    if (ch == '=') { AddCh(); goto case 81; }
                    else { t.kind = 127; break; }
                case 199:
                    if (ch == '<') { AddCh(); goto case 186; }
                    else if (ch == '=') { AddCh(); goto case 194; }
                    else { t.kind = 100; break; }
                case 200:
                    if (ch == '=') { AddCh(); goto case 91; }
                    else { t.kind = 128; break; }
                case 201:
                    if (ch == '=') { AddCh(); goto case 93; }
                    else if (ch == '|') { AddCh(); goto case 192; }
                    else { t.kind = 124; break; }
                case 202:
                    if (ch == '?') { AddCh(); goto case 191; }
                    else { t.kind = 110; break; }
                case 203:
                    if (ch == '=') { AddCh(); goto case 101; }
                    else { t.kind = 125; break; }

            }
            t.val = new String(tval, 0, tlen);
            return t;
        }

        // get the next token (possibly a token already seen during peeking)
        public Token Scan()
        {
            if (tokens.next == null)
            {
                return NextToken();
            }
            else
            {
                pt = tokens = tokens.next;
                return tokens;
            }
        }

        // peek for the next token, ignore pragmas
        public Token Peek()
        {
            if (pt.next == null)
            {
                do
                {
                    pt = pt.next = NextToken();
                } while (pt.kind > maxT); // skip pragmas
            }
            else
            {
                do
                {
                    pt = pt.next;
                } while (pt.kind > maxT);
            }
            return pt;
        }

        // make sure that peeking starts at the current scan position
        public void ResetPeek() { pt = tokens; }

    } // end Scanner

} // end namespace
