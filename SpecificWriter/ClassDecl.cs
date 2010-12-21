using Reflector.CodeModel;
using Gen=System.Collections.Generic;
using LanguageWriter=Reflector.Languages.CppCliLanguage.LanguageWriter;
using Interop=System.Runtime.InteropServices;
using Rgx=System.Text.RegularExpressions;

namespace mwg.Reflector.CppCli{
	public class NameMangling{
		public static string UnDecorateName(string name){
			if(name.Length==0)return "";

			if(name[0]=='$'){
				if(name.StartsWith("$ArrayType$")){
					// case: 配列型
					name="?a@@3"+name.Substring(11)+"?";
					name=UnDecorateSymbolName(name).Replace("()","");
					return "<"+name+">";
				}else if(name.StartsWith("$PTMType$")){
					// case: メンバへのポインタ (Pointer To Member)
					name="?a@@3"+name.Substring(9)+"?";
					name=UnDecorateSymbolName(name);
					return "<"+name+">";
				}
			}else if(name[0]=='?'){
				// case: ??__E / ??__F の接頭辞 ←何を意味するのか?
				if(name.StartsWith("??__")){
					if(name.Length>=12&&(name[4]=='E'||name[4]=='F')&&name.EndsWith("@@YMXXZ")){
						string specialName="";
						if(name[4]=='E')
							specialName="static member constructor"; // ←憶測
						else if(name[4]=='F')
							specialName="static member destructor"; // ←憶測

						name=UnDecorateName(name.Substring(5,name.Length-12));

						return "void __clrcall `"+specialName+"'<"+name+">(void)";
					}
				}

				// case: ?A0xXXXXXXXX. の接頭辞
				Rgx::Match m=Rgx::Regex.Match(name,@"\?A(0x[a-fA-F0-9]{8})\.");
				if(m.Success){
					name="["+m.Groups[1].Value+"]"+UnDecorateName(name.Substring(13));
					return name;
				}

				// case: その他
				name=Rgx::Regex.Replace(name.Replace("$$Q",""),@"\<\b(\w+)\b\>","_langle_$1_rangle_");
				name=UnDecorateSymbolName(name);
				name=Rgx::Regex.Replace(name,@"_langle_(\w+)_rangle_","<$1>");
				return name;
			}

			return name;
		}

		private static string UnDecorateSymbolName(string decoratedName){
			System.Text.StringBuilder buffer=new System.Text.StringBuilder(1024);
			const uint UNDNAME_COMPLETE=0;
			UnDecorateSymbolName(decoratedName,buffer,(uint)buffer.Capacity,UNDNAME_COMPLETE);
			return buffer.ToString();
		}

		[Interop::DllImport("imagehlp",CharSet=Interop::CharSet.Ansi)]
		private static extern uint UnDecorateSymbolName(
			[Interop::MarshalAs(Interop::UnmanagedType.LPStr)]string decoratedName,
			[Interop::MarshalAs(Interop::UnmanagedType.LPStr)]System.Text.StringBuilder unDecoratedName,
			uint buffSize,
			uint flags
			);
	}
}
