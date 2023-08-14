namespace PhpUpgrader.Rubicon.UpgradeExtensions;

/// <summary>
/// Warning: mktime() expects parameter 5 to be int, string given in /var/www/vhosts/vestavne-spotrebice.cz/rubicon/modules/sety/sety_darky_zdarma.php on line 156
/// </summary>
public static partial class Mktime
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MA0011:IFormatProvider is missing", Justification = "Basic param check with int.TryParse")]
    public static FileWrapper UpgradeMktime(this FileWrapper file)
    {
        var mktimeIndex = file.Content.IndexOf("mktime");
        while (mktimeIndex > -1)
        {
            var sb = new StringBuilder("mktime(");
            var bracketScope = 1;
            var paramsCount = 0;
            var paramStartIndex = file.Content.IndexOf('(', mktimeIndex) + 1;
            int i;
            for (i = paramStartIndex; bracketScope > 0; i++)
            {
                switch (file.Content[i])
                {
                    case '(': bracketScope++; break;
                    case ')':
                        if (--bracketScope == 0)
                        {
                            _AppendParameter(sb, ref paramStartIndex, i);
                            paramsCount++;
                            sb.Append(')');
                        }
                        break;
                    case ',':
                        if (bracketScope == 1)
                        {
                            _AppendParameter(sb, ref paramStartIndex, i);
                            if (++paramsCount < 6)
                            {
                                sb.Append(", ");
                            }
                        }
                        break;
                }
            }
            if (paramsCount < 6)
            {
                mktimeIndex = file.Content.IndexOf("mktime", mktimeIndex + 1);
                continue;
            }
            file.Content.Remove(mktimeIndex, i - mktimeIndex);
            file.Content.Insert(mktimeIndex, sb);
            mktimeIndex = file.Content.IndexOf("mktime", mktimeIndex + sb.Length);
        }
        return file;

        void _AppendParameter(StringBuilder sb, ref int startIndex, int endIndex)
        {
            var count = endIndex - startIndex;
            if (count == 0)
                return;

            Span<char> param = stackalloc char[count];
            file.Content.CopyTo(startIndex, param, count);
            param = param.Trim();

            var isInt = int.TryParse(param, out _);
            sb.Append(isInt ? param : $"intval({param})");

            startIndex = endIndex + 1;
        }
    }
}
