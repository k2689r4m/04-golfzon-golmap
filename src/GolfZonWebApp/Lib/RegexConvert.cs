using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GolfZonWebApp.Lib
{
    public class RegexConvert
    {
        static char[] chr = { 'ㄱ', 'ㄲ', 'ㄴ', 'ㄷ', 'ㄸ', 'ㄹ', 'ㅁ', 'ㅂ', 'ㅃ', 'ㅅ', 'ㅆ', 'ㅇ', 'ㅈ', 'ㅉ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ' };
        static string[] str = { "가", "까", "나", "다", "따", "라", "마", "바", "빠", "사", "싸", "아", "자", "짜", "차", "카", "타", "파", "하" };
        static int[] chrint = { 44032, 44620, 45208, 45796, 46384, 46972, 47560, 48148, 48736, 49324, 49912, 50500, 51088, 51676, 52264, 52852, 53440, 54028, 54616, 55204 };

        public static string Regex (string search)
        {
            string regex = "";

            for (int i = 0; i < search.Length; i++)
            {
                //초성만 입력되었을때
                if (search[i] >= 'ㄱ' && search[i] <= 'ㅎ')
                {
                    for (int j = 0; j < chr.Length; j++)
                    {
                        if (search[i] == chr[j])
                        {
                            regex += string.Format("[{0}-{1}]", str[j], (char)(chrint[j + 1] - 1));
                        }
                    }
                }
                //완성된 문자를 입력했을때 검색패턴 쓰기
                else if (search[i] >= '가')
                {
                    //받침이 있는지 검사
                    int magic = ((search[i] - '가') % 588);

                    //받침이 없을때.
                    if (magic == 0)
                    {
                        regex += string.Format("[{0}-{1}]", search[i], (char)(search[i] + 27));
                    }

                    //받침이 있을때
                    else
                    {
                        magic = 27 - (magic % 28);
                        regex += string.Format("[{0}-{1}]", search[i], (char)(search[i] + magic));
                    }
                }
                //영어를 입력했을때
                else if (search[i] >= 'A' && search[i] <= 'z')
                {
                    regex += search[i];
                }
                //숫자를 입력했을때.
                else if (search[i] >= '0' && search[i] <= '9')
                {
                    regex += search[i];
                }
                //특수문자를 입력했을때. 2021-08-19 추가함
                else
                {
                    regex += search[i];
                }
            }

            return regex;
        }
    }
}
