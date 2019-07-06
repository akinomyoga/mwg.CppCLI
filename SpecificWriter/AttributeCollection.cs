using Reflector.CodeModel;
using Gen=System.Collections.Generic;
using LanguageWriter=Reflector.Languages.CppCliLanguage.LanguageWriter;
using AttrPair=System.Collections.Generic.KeyValuePair<string,Reflector.CodeModel.ICustomAttribute>;

namespace mwg.Reflector.CppCli{
	/// <summary>
	/// ����̃J�X�^�������ɑ΂��鏈�����s���܂��B
	/// </summary>
	/// <param name="attr">�o�͂��悤�Ƃ��Ă���J�X�^���������w�肵�܂��B</param>
	/// <returns>�w��̑������o�͂��邩�ǂ������w�肵�܂��B
	/// true ��Ԃ����ꍇ�o�͂��s���܂��B
	/// false ��Ԃ����ꍇ�o�͂��s���܂���B</returns>
	public delegate bool CustomAttributeProc(string attrName,ICustomAttribute attr);
	/// <summary>
	/// �J�X�^���������X�g���o�͂���ׂ̃N���X�ł��B
	/// </summary>
	internal class AttributeCollection{
		LanguageWriter writer;
		ICustomAttributeProvider provider;
		IType type;

		Gen::List<AttrPair> attrs=new Gen::List<AttrPair>();
		bool containsParams=false;
		bool isAsmOrModule=false;
		string attr_class=null;
		//============================================================
		//	������
		//============================================================
		public AttributeCollection(LanguageWriter writer,ICustomAttributeProvider provider,IType type){
			this.writer=writer;
			this.provider=provider;
			this.type=type;

			// �����̑Ώ�
			if(provider is IAssembly){
				attr_class="assembly";
				isAsmOrModule=true;
			}else if(provider is IModule){
				attr_class="module";
				isAsmOrModule=true;
			}else if(provider is IMethodReturnType){
				attr_class="returnvalue";
			}

			// �X�̑����ɑ΂��đ���
			foreach(ICustomAttribute attr in provider.Attributes){
				if(attr==null)continue;

				string attrname=GetCustomAttributeName(attr);
				switch(attrname){
					case "ParamArray":
					case "System::ParamArray":
						containsParams=true;
						continue;
				
					case "MarshalAs":
					case "System::Runtime::InteropServices::MarshalAs":
						IExpressionCollection arguments=attr.Arguments;
						if(arguments==null)break;
						IFieldReferenceExpression exp_fld=arguments[0] as IFieldReferenceExpression;
						if(exp_fld==null)break;
						ITypeReferenceExpression target=exp_fld.Target as ITypeReferenceExpression;
						if(target==null||target.Type.Name!="UnmanagedType")break;
						IFieldReference field=exp_fld.Field;
						if(field.Name=="U1"){
							if(LanguageWriter.Type(type,"System","Boolean")) {
								continue;
							}
						}else if(field.Name=="U2"&&LanguageWriter.Type(type,"System","Char")){
							continue;
						}
						break;
				}

				attrs.Add(new AttrPair(attrname,attr));
			}

			// ���O��ԏ��ɕ��ёւ�
			attrs.Sort(delegate(AttrPair l,AttrPair r){
				string l_name=((ITypeReference)l.Value.Constructor.DeclaringType).Namespace;
				string r_name=((ITypeReference)r.Value.Constructor.DeclaringType).Namespace;
				return l_name.CompareTo(r_name);
			});
		}
		//============================================================
		//	�����o��
		//============================================================
		private CustomAttributeProc attr_proc=null;
		public void Write(CustomAttributeProc attrProc){
			this.attr_proc=attrProc;
			this.Write();
		}

		public void Write(){
			if(containsParams)writer.Write("... ");

			if(isAsmOrModule){
				this.WriteForAsmOrModule();
			}else if(!writer.SkipWriteLine){
				this.WriteWithLine();
			}else{
				this.WriteSkipLine();
			}
		}

		private void WriteForAsmOrModule(){
			foreach(AttrPair pair in attrs){
				if(attr_proc!=null&&!attr_proc(pair.Key,pair.Value))continue;
				ICustomAttribute attr=pair.Value;

				writer.Write("[");
				writer.WriteKeyword(attr_class);
				writer.Write(": ");
				this.WriteCustomAttribute(attr);
				writer.Write("]");
				writer.WriteLine();
			}
		}

		private void WriteSkipLine(){
			writer.Write("[");
			if(attr_class!=null){
				writer.WriteKeyword(attr_class);
				writer.Write(": ");
			}

			bool first=true;
			foreach(AttrPair pair in attrs) {
				if(attr_proc!=null&&!attr_proc(pair.Key,pair.Value))continue;
				ICustomAttribute attr=pair.Value;

				if(first)first=false;else writer.Write(", ");
				this.WriteCustomAttribute(attr);
			}
			writer.Write("]");
		}

		private void WriteWithLine(){
			bool new_ns=true;
			string prev_ns=null;
			foreach(AttrPair pair in attrs) {
				if(attr_proc!=null&&!attr_proc(pair.Key,pair.Value))continue;
				ICustomAttribute attr=pair.Value;

				// ���O��Ԃ̕ς���
				string ns=((ITypeReference)attr.Constructor.DeclaringType).Namespace;
				if(prev_ns!=ns){
					prev_ns=ns;
					if(!new_ns){
						writer.Write("]");
						writer.WriteLine();
						new_ns=true;
					}
				}

				// �V�������O���
				if(new_ns){
					writer.Write("[");
					if(attr_class!=null){
						writer.WriteKeyword(attr_class);
						writer.Write(": ");
					}
					new_ns=false;
				}else{
					writer.Write(", ");
				}

				this.WriteCustomAttribute(attr);
			}

			if(prev_ns!=null)
				writer.Write("]");
		}

		private void WriteCustomAttribute(ICustomAttribute attr){
			IMethodReference meth_ref=attr.Constructor;
			TypeRef decltype=new TypeRef(meth_ref.DeclaringType);
			string name=decltype.Name;

			// �Q�ƕt���Ŗ��O���w��
			writer.WriteReference(
				name.EndsWith("Attribute")?name.Substring(0,name.Length-9):name,
				string.Format("/* ���� �R���X�g���N�^ */\r\n{0}::{1}({2});",
					decltype.FullName,
					decltype.Name,
					LanguageWriter.GetDesc(meth_ref.Parameters)
				),
				meth_ref
			);

			if(attr.Arguments.Count!=0){
				writer.Write("(");
				writer.WriteExpressionCollection(attr.Arguments);
				writer.Write(")");
			}
		}
		//============================================================
		//	Util
		//============================================================
		private static string GetCustomAttributeName(ICustomAttribute iCustomAttribute) {
			string name=new TypeRef(iCustomAttribute.Constructor.DeclaringType).Name;
			if(name.EndsWith("Attribute")){
				name=name.Substring(0,name.Length-9);
			}
			return name;
		}
	}
}