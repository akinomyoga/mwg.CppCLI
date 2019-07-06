using Reflector;
using Reflector.CodeModel;
using Gen=System.Collections.Generic;
using LanguageWriter=Reflector.Languages.CppCliLanguage.LanguageWriter;

namespace mwg.Reflector.CppCli{
	public struct TypeRef{
		IType type;
		ITypeReference tref;

		static Gen::Dictionary<string,string> specialTypeNames
			=new System.Collections.Generic.Dictionary<string,string>();
		static TypeRef(){
			specialTypeNames["Void"]="void";
			specialTypeNames["Boolean"]="bool";
			specialTypeNames["Char"]="wchar_t";
			specialTypeNames["SByte"]="char";
			specialTypeNames["Byte"]="unsigned char";
			specialTypeNames["Int16"]="short";
			specialTypeNames["UInt16"]="unsigned short";
			specialTypeNames["Int32"]="int";
			specialTypeNames["UInt32"]="unsigned int";
			specialTypeNames["Int64"]="__int64"; // long long
			specialTypeNames["UInt64"]="unsigned __int64"; // unsigned long long
			specialTypeNames["Single"]="float";
			specialTypeNames["Double"]="double";

			specialTypeNames["IntPtr"]="IntPtr";
			specialTypeNames["UIntPtr"]="UIntPtr";

			InitializeTypeToRefs();
		}
		public TypeRef(IType type){
			this.type=type;
			this.tref=type as ITypeReference;
		}
		public TypeRef(ITypeReference tref){
			this.type=null;
			this.tref=tref;
		}

		public bool IsRefType{
			get{return tref!=null?!tref.ValueType&&!IsPrimitive:type is IArrayType;}
		}
		public bool IsValueType{
			get{return tref!=null?tref.ValueType:!(type is IArrayType)&&!IsPointer;}
		}
		public bool IsPrimitive{
			get{return tref!=null&&tref.Namespace=="System"&&specialTypeNames.ContainsKey(tref.Name);}
		}
		public bool IsVoid{
			get{return tref!=null&&tref.Namespace=="System"&&tref.Name=="Void";}
		}
		public bool IsPointer{
			get{return type is IPointerType;}
		}

		public bool IsType(string nameSpace,string name){
			return this.tref!=null&&tref.Name==name&&tref.Namespace==nameSpace;
		}
		public bool IsFunctionPointer{
			get{return type is IFunctionPointer;}
		}
		public bool IsEmpty{
			get{return type==null&&tref==null;}
		}
		public bool IsObject{
			get{return IsType("System","Object");}
		}
		//===========================================================
		//		���̌^�̕]��
		//===========================================================

		#region Expression �̕Ԃ��^
		public static TypeRef GetReturnType(IExpression exp){
			switch(CppCli.ExpressionWriter.GetExpressionType(exp)){
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.AddressDereference:
					TypeRef ret=GetReturnType(((IAddressDereferenceExpression)exp).Expression);
					if(!ret.IsPointer)goto default;
					return new TypeRef(((IPointerType)ret.type).ElementType);
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.AddressOf:
					return (CPointerType)GetReturnType(((IAddressDereferenceExpression)exp).Expression);
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.AnonymousMethod:	// delegate �^
					return new TypeRef(((IAnonymousMethodExpression)exp).DelegateType);
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.ArgumentReference:
					return new TypeRef(((IArgumentReferenceExpression)exp).Parameter.Resolve().ParameterType);
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.ArrayCreate:
					return new CArrayType((IArrayCreateExpression)exp);
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.ArrayIndexer:
					IArrayType atype=GetReturnType(((IArrayIndexerExpression)exp).Target).type as IArrayType;
					if(atype==null)goto default;
					return new TypeRef(atype.ElementType);
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.Assign:
					return GetReturnType(((IAssignExpression)exp).Target);
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.Binary:
					IBinaryExpression exp_bi=(IBinaryExpression)exp;
					switch(exp_bi.Operator){
						case BinaryOperator.BooleanAnd:
						case BinaryOperator.BooleanOr:
						case BinaryOperator.GreaterThan:
						case BinaryOperator.GreaterThanOrEqual:
						case BinaryOperator.IdentityEquality:
						case BinaryOperator.IdentityInequality:
						case BinaryOperator.LessThan:
						case BinaryOperator.LessThanOrEqual:
						case BinaryOperator.ValueEquality:
						case BinaryOperator.ValueInequality:
							return GetReturnType_fromType(typeof(bool));
						default:
							return GetReturnType(exp_bi.Left);
					}
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.CanCast:
					return GetReturnType_fromType(typeof(bool));
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.Cast:
					return new TypeRef(((ICastExpression)exp).TargetType);
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.Condition:
					return GetReturnType(((IConditionExpression)exp).Then);
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.DelegateCreate:
					return new TypeRef(((IDelegateCreateExpression)exp).DelegateType);
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.EventReference:
					return new TypeRef(((IEventReferenceExpression)exp).Event.EventType);
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.FieldReference:
					return new TypeRef(((IFieldReferenceExpression)exp).Field.FieldType);
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.Literal:
					object v=((ILiteralExpression)exp).Value;
					if(v==null)goto default;
					return GetReturnType_fromType(v.GetType());
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.MethodInvoke:
					return GetReturnType(((IMethodInvokeExpression)exp).Method);
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.MethodReference:	// ���\�b�h
					return new TypeRef(((IMethodReferenceExpression)exp).Method.ReturnType.Type);
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.ObjectCreate:
					return new TypeRef(((IObjectCreateExpression)exp).Type);
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.PropertyIndexer:
					exp=((IPropertyIndexerExpression)exp).Target;
					goto PropertyReference;
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.PropertyReference:
				PropertyReference:
					return new TypeRef(((IPropertyReferenceExpression)exp).Property.PropertyType);
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.SizeOf:
					return GetReturnType_fromType(typeof(int));
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.StackAlloc:
					return (CPointerType)new TypeRef(((IStackAllocateExpression)exp).Type);
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.TryCast:
					return new TypeRef(((ITryCastExpression)exp).TargetType);
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.TypeOf:
					return GetReturnType_fromType(typeof(System.Type));
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.Unary:
					return GetReturnType(((IUnaryExpression)exp).Expression);
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.VariableReference:
					return new TypeRef(((IVariableReferenceExpression)exp).Variable.Resolve().VariableType);
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.VariableDeclaration:	// void?
					return new TypeRef(((IVariableDeclarationExpression)exp).Variable.VariableType);
				//---------------------------------------------------
				//		������
				//---------------------------------------------------
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.ThisReference:	// const <���̎��̌^>*
				//---------------------------------------------------
				//		�������Ȃ� or ��
				//---------------------------------------------------
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.AddressOut:		// ?
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.AddressReference:	// ?
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.ArgumentList:		// ���X�g
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.BaseReference:	// �^
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.Block:			// ���X�g
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.DelegateInvoke:
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.FieldOf:			// ?
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.GenericDefault:	// ?
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.Lambda:			// ?
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.MemberInitializer:// void?
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.MethodOf:			// ?
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.NullCoalescing:	// ? (?? �̎�?)
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.Query:			// ?
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.Snippet:			// ?
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.TypedReferenceCreate:
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.TypeOfTypedReference:
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.TypeReference:
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.Unknown:
				case mwg.Reflector.CppCli.ExpressionWriter.ExpressionType.ValueOfTypedReference:
				default:
					return new TypeRef();
			}
		}
		private class CPointerType:IPointerType{
			private IType elem;
			public IType ElementType{
				get{return elem;}
				set{this.elem=value;}
			}
			public int CompareTo(object obj) {
				IPointerType ptype=obj as IPointerType;
				return ptype==null?-1:this.elem.CompareTo(ptype.ElementType);
			}

			private CPointerType(IType elem) {this.elem=elem;}

			public static explicit operator CPointerType(TypeRef elem) {
				return new CPointerType(elem.type);
			}
			public static implicit operator TypeRef(CPointerType val) {
				return new TypeRef(val);
			}
		}
		private class CArrayType:IArrayType{
			private IType elem;
			public IType ElementType {
				get{return elem;}
				set {elem=value;}
			}

			private IArrayDimensionCollection dim;
			public IArrayDimensionCollection Dimensions {
				get {return dim;}
			}

			public int CompareTo(object obj) {
				IArrayType atype=obj as IArrayType;
				if(atype==null)return -1;

				int ret=this.ElementType.CompareTo(atype.ElementType);
				if(ret!=0)return ret;

				ret=this.Dimensions.Count-atype.Dimensions.Count;
				if(ret!=0)return ret;

				for(int i=0,iM=dim.Count;i<iM;i++){
					ret=dim[i].LowerBound-atype.Dimensions[i].LowerBound;
					if(ret!=0)return ret;
					ret=dim[i].UpperBound-atype.Dimensions[i].UpperBound;
					if(ret!=0)return ret;
				}

				return 0;
			}

			private CArrayType(IType elem,IArrayDimensionCollection dim){
				this.elem=elem;
				this.dim=dim;
			}
			public CArrayType(IArrayCreateExpression exp)
				:this(exp.Type,new CDimensionCollection(exp.Dimensions)){}
			public static implicit operator TypeRef(CArrayType value){
				return new TypeRef(value);
			}

			private class CDimensionCollection:Gen::List<IArrayDimension>,IArrayDimensionCollection{
				public CDimensionCollection(IExpressionCollection c):base(c.Count){
					for(int i=0,iM=c.Count;i<iM;i++)
						this[i]=new CDimension(); // ���̂Ƃ���͑S�� 0
				}

				public void AddRange(System.Collections.ICollection value) {
					foreach(IArrayDimension item in value)
						base.Add(item);
				}

				public new void Remove(IArrayDimension value) {
					base.Remove(value);
				}
			}

			private class CDimension:IArrayDimension{
				private int l=0;
				private int u=0;

				public CDimension(){}

				public int LowerBound{
					get{return l;}
					set{this.l=value;}
				}
				public int UpperBound {
					get{return u;}
					set{this.u=value;}
				}
			}
		}
		private static TypeRef GetReturnType_fromType(System.Type t){
			TypeRef ret;
			type_refs.TryGetValue(t,out ret);
			return ret; // ���s�����ꍇ�� default(TypeRef) �������Ă���̂� OK
		}
//#warning System.Type -> TypeRef : �V�������ŏ���������̂�?
		private static Gen::Dictionary<System.Type,TypeRef> type_refs
			=new System.Collections.Generic.Dictionary<System.Type,TypeRef>();
		private static void InitializeTypeToRefs(){
			type_refs.Add(typeof(bool),new CTypeFromClrType(typeof(bool)));
			type_refs.Add(typeof(sbyte),new CTypeFromClrType(typeof(sbyte)));
			type_refs.Add(typeof(byte),new CTypeFromClrType(typeof(byte)));
			type_refs.Add(typeof(short),new CTypeFromClrType(typeof(short)));
			type_refs.Add(typeof(ushort),new CTypeFromClrType(typeof(ushort)));
			type_refs.Add(typeof(int),new CTypeFromClrType(typeof(int)));
			type_refs.Add(typeof(uint),new CTypeFromClrType(typeof(uint)));
			type_refs.Add(typeof(long),new CTypeFromClrType(typeof(long)));
			type_refs.Add(typeof(ulong),new CTypeFromClrType(typeof(ulong)));
			type_refs.Add(typeof(float),new CTypeFromClrType(typeof(float)));
			type_refs.Add(typeof(double),new CTypeFromClrType(typeof(double)));
			type_refs.Add(typeof(decimal),new CTypeFromClrType(typeof(decimal)));
			type_refs.Add(typeof(char),new CTypeFromClrType(typeof(char)));
			type_refs.Add(typeof(string),new CTypeFromClrType(typeof(string)));
		}
		private sealed class CTypeFromClrType:IType,ITypeReference{
			public System.Type type;
			public string ns;
			public string name;
			public CTypeFromClrType(System.Type type){
				this.type=type;
				this.ns=type.Namespace;
				this.name=type.Name;
			}
			public static implicit operator TypeRef(CTypeFromClrType t){
				return new TypeRef((IType)t);
			}

			public int CompareTo(object obj){
				bool r=false;
				if(obj is CTypeFromClrType){
					r=this.type==((CTypeFromClrType)obj).type;
				}else if(obj is IType){
					r=new TypeRef((IType)obj).IsType(ns,name);
				}else if(obj is ITypeReference){
					r=new TypeRef((ITypeReference)obj).IsType(ns,name);
				}else if(obj is TypeRef){
					r=((TypeRef)obj).IsType(ns,name);
				}else if(obj is System.Type){
					r=this.type==(System.Type)obj;
				}
				return r?0:-1;
			}
			public ITypeReference GenericType {
				get{return null;}
				set{throw new System.Exception("The method or operation is not implemented.");}
			}

			public string Name {
				get{return ns;}
				set{ns=value;}
			}

			public string Namespace {
				get{return name;}
				set{name=value;}
			}

			public object Owner {
				get{return null;}
				set{throw new System.Exception("The method or operation is not implemented.");}
			}

			public ITypeDeclaration Resolve(){
				throw new System.Exception("The method or operation is not implemented.");
			}

			public bool ValueType {
				get {return type.IsValueType;}
				set {throw new System.Exception("The method or operation is not implemented.");}
			}

			public ITypeCollection GenericArguments {
				get {throw new System.Exception("The method or operation is not implemented.");}
			}

			#region IMetadataItem �����o
			// �V���� ITypeReference �ɃC���^�[�t�F�C�X���ǉ����ꂽ�l�ł��邪�A���Ɏg�����Ȃ̂�������Ȃ��B
			// �Ƃ������A���̃N���X�̃C���X�^���X�́A
			// ���̃A�Z���u���̃R�[�h�ɓn��Ȃ��̂ŁA�����͂ǂ��ł������B
			int IMetadataItem.Token{
				get { throw new System.NotImplementedException(); }
			}
			#endregion

		}
		#endregion

		//===========================================================
		//		����
		//===========================================================

		#region �^�̖��O�̏���
		/// <summary>
		/// �^�����������݂܂��B�Q�ƌ^�̏ꍇ�ɂ� ^ ����ɑ����܂��B�l�^�̏ꍇ�ɂ͉����t���܂���B
		/// </summary>
		/// <param name="w">�������݂Ɏg�p���� LanguageWriter ���w�肵�܂��B</param>
		public void WriteNameWithRef(LanguageWriter w){
			this.WriteName(w);
			if(IsRefType)w.Write("^");
		}
		public string WriteName(LanguageWriter w) {
			if(tref!=null){
				string name=tref.Name.Replace(".","::").Replace("+","::");

				string special;
				if(tref.Namespace=="System"&&specialTypeNames.TryGetValue(name,out special)){
					name=special;
				}

				name=NameMangling.UnDecorateName(name);

				w.WriteReference(name,this.FullName,tref);

				ITypeCollection genericArguments=tref.GenericArguments;
				if(genericArguments.Count>0) {
					w.Write("<");
					bool first=true;
					foreach(IType type1 in genericArguments) {
						if(first)first=false;else w.Write(", ");
						new TypeRef(type1).WriteNameWithRef(w);
					}
					w.Write(">");
				}

				return name;
			}

			IArrayType type2=type as IArrayType;
			if(type2!=null){
				w.WriteKeyword("array");
				w.Write("<");
				new TypeRef(type2.ElementType).WriteNameWithRef(w);

				if(type2.Dimensions.Count>1){
					w.Write(", ");
					w.WriteAsLiteral(type2.Dimensions.Count);
				}
				w.Write(">");
			}

			IPointerType type4=type as IPointerType;
			if(type4!=null) {
				new TypeRef(type4.ElementType).WriteNameWithRef(w);
				w.Write("*");
			}

			IReferenceType type3=type as IReferenceType;
			if(type3!=null) {
				new TypeRef(type3.ElementType).WriteNameWithRef(w);
				w.Write("%");
			}

			IOptionalModifier modifier2=type as IOptionalModifier;
			if(modifier2!=null) {
				WriteModify(w,new TypeRef(modifier2.Modifier),new TypeRef(modifier2.ElementType),false);
			}

			IRequiredModifier modifier=type as IRequiredModifier;
			if(modifier!=null) {
				WriteModify(w,new TypeRef(modifier.Modifier),new TypeRef(modifier.ElementType),true);
			}

			IFunctionPointer fptr=type as IFunctionPointer;
			if(fptr!=null) {
				w.Write("(");
				new TypeRef(fptr.ReturnType.Type).WriteNameWithRef(w);
				w.Write(" (*)(");
				bool first=true;
				foreach(IParameterDeclaration declaration in fptr.Parameters){
					if(first)first=false;else w.Write(", ");
					new TypeRef(declaration.ParameterType).WriteNameWithRef(w);
				}
				w.Write("))");
			}

			IGenericParameter parameter=type as IGenericParameter;
			if(parameter!=null){
				w.WriteReference(parameter.Name,"/* �W�F�l���b�N�p�����[�^ */ "+parameter.Name,null);
			}

			IGenericArgument argument=type as IGenericArgument;
			if(argument!=null){
				new TypeRef(argument.Resolve()).WriteNameWithRef(w);
			}

			if(type is IValueTypeConstraint){
				w.WriteKeyword("value class");
			}else if(type is IDefaultConstructorConstraint) {
				w.Write("<DefaultConstructorConstraint>");
			}

			return "<�s���Ȍ^>";
		}
		private static void WriteModify(LanguageWriter w,TypeRef modifier,TypeRef elemType,bool required){
			if(modifier.tref!=null&&modifier.tref.Namespace=="System.Runtime.CompilerServices"){
				switch(modifier.tref.Name){
					case "IsVolatile":
						if(elemType.IsPointer){
							elemType.WriteNameWithRef(w);
							w.WriteKeyword("volatile");
						}else{
							w.WriteKeyword("volatile");
							w.Write(" ");
							elemType.WriteNameWithRef(w);
						}
						return;
					case "IsConst":
						if(elemType.IsPointer){
							elemType.WriteNameWithRef(w);
							w.WriteKeyword("const");
						}else{
//#warning RefOnStack �̏ꍇ�ɂ� const �͗v��Ȃ� �� refOnStack �ŌĂяo�����ŁA�����ŔV����菜���悤�ɂ���
							w.WriteKeyword("const");
							w.Write(" ");
							elemType.WriteNameWithRef(w);
						}
						return;
					case "IsLong":
						if(elemType.IsType("System","Int32")){
							w.WriteReference("long",elemType.FullName,elemType.tref);
							return;
						}else if(elemType.IsType("System","UInt32")){
							w.WriteReference("unsigned long",elemType.FullName,elemType.tref);
							return;
						}else if(elemType.IsType("System","Double")){
							w.WriteReference("long double",elemType.FullName,elemType.tref);
							return;
						}
						break;
					case "IsExplicitlyDereferenced":
						IReferenceType reftype=elemType.type as IReferenceType;
						if(reftype!=null){
							w.WriteKeyword("pin_ptr");
							w.Write("<");
							new TypeRef(reftype.ElementType).WriteNameWithRef(w);
							w.Write(">");
							return;
						}
						break;
					case "CallConvStdcall":
						elemType.WriteNameWithRef(w);
						w.Write(" ");
						w.WriteKeyword("__stdcall");
						return;
					case "CallConvCdecl":
						elemType.WriteNameWithRef(w);
						w.Write(" ");
						w.WriteKeyword("__cdecl");
						return;
					case "CallConvFastcall":
						elemType.WriteNameWithRef(w);
						w.Write(" ");
						w.WriteKeyword("__fastcall");
						return;
					case "CallConvThiscall":
						elemType.WriteNameWithRef(w);
						w.Write(" ");
						w.WriteKeyword("__thiscall");
						return;
					case "IsBoxed":
						IOptionalModifier mod=elemType.type as IOptionalModifier;
						if(mod==null)break;

						TypeRef modifier2=new TypeRef(mod.Modifier);
						if(!modifier2.IsValueType)break;
						
						if(!new TypeRef(mod.ElementType).IsType("System","ValueType"))break;
						
						modifier2.WriteName(w);w.Write("^");
						return;
				}
			}

			elemType.WriteNameWithRef(w);
			w.Write("<");
			w.WriteKeyword(required?"modreq":"modopt");
			w.Write(" ");
			w.WriteReference(modifier.Name,modifier.FullName,modifier.tref);
			w.Write(">");
			return;
		}
		#endregion

		//===========================================================
		//		�^��
		//===========================================================

		#region �^�̖��O�̎擾
		public string FullName{
			get{return GetName(true);}
		}
		public string FullNameWithRef{
			get{return GetNameWithRef(true);}
		}
		public string Name{
			get{return GetName(false);}
		}
		public string NameWithRef{
			get{return GetNameWithRef(false);}
		}
		private static string Modify(TypeRef modifier,TypeRef elemType,bool fullname,bool required){
			string fix;
			if(modifier.tref!=null&&modifier.tref.Namespace=="System.Runtime.CompilerServices") {
				switch(modifier.tref.Name) {
					case "CallConvStdcall":		fix=" __stdcall";goto postfix;
					case "CallConvCdecl":		fix=" __cdecl";goto postfix;
					case "CallConvFastcall":	fix=" __fastcall";goto postfix;
					case "CallConvThiscall":	fix=" __thiscall";goto postfix;
					case "IsVolatile":
						if(elemType.IsPointer) {
							fix="volatile";goto postfix;
						}else{
							fix="volatile ";goto prefix;
						}
					case "IsConst":
						if(elemType.IsPointer) {
							fix="const";goto postfix;
						}else{
							fix="const ";goto prefix;
						}
					case "IsLong":
						if(elemType.IsType("System","Int32")) {
							return "long";
						} else if(elemType.IsType("System","UInt32")) {
							return "unsigned long";
						} else if(elemType.IsType("System","Double")) {
							return "long double";
						}
						break;
					case "IsExplicitlyDereferenced":
						IReferenceType reftype=elemType.type as IReferenceType;
						if(reftype!=null) {
							return "pin_ptr<"+new TypeRef(reftype.ElementType).GetNameWithRef(fullname)+">";
						}
						break;
					case "IsBoxed":
						IOptionalModifier mod=elemType.type as IOptionalModifier;
						if(mod==null)break;

						TypeRef modifier2=new TypeRef(mod.Modifier);
						if(!modifier2.IsValueType) break;

						if(!new TypeRef(mod.ElementType).IsType("System","ValueType"))break;

						return modifier2.GetName(fullname)+"^";
				}
			}

			fix="<"+(required?"modreq":"modopt")+" "+modifier.Name+">";
		postfix:
			return elemType.GetNameWithRef(fullname)+fix;
		prefix:
			return fix+elemType.GetNameWithRef(fullname);
		}
		/// <summary>
		/// �Ăяo������ tref!=null
		/// </summary>
		private string RawNamespace{
			get{
				if(tref.Namespace==""){
					IType t=tref.Owner as IType;
					if(t!=null){
						return new TypeRef(t).FullName+"::";
					}else{
						return "";
					}
				}else{
					return tref.Namespace.Replace(".","::").Replace("+","::")+"::";
				}
			}
		}
		private string GetNameWithRef(bool fullname){
			string ret=GetName(fullname);
			if(IsRefType)ret+="^";
			return ret;
		}
		private string GetName(bool fullname){
			if(tref!=null){
				string name=tref.Name.Replace(".","::").Replace("+","::");

				if(!fullname){
					string special;
					if(tref.Namespace=="System"&&specialTypeNames.TryGetValue(name,out special)){
						name=special;
					}
				}

				ITypeCollection genericArguments=tref.GenericArguments;
				if(genericArguments.Count>0) {
					System.Text.StringBuilder build=new System.Text.StringBuilder(name);
					
					build.Append('<');
					bool first=true;
					foreach(IType type1 in genericArguments) {
						if(first)first=false;else build.Append(", ");
						build.Append(new TypeRef(type1).GetNameWithRef(fullname));
					}
					build.Append('>');

					name=build.ToString();
				}

				return fullname?RawNamespace+name:name;
			}

			IArrayType type2=type as IArrayType;
			if(type2!=null){
				string name=type2.Dimensions.Count<=1?"":", "+type2.Dimensions.Count.ToString();
				return "array<"+new TypeRef(type2.ElementType).GetNameWithRef(fullname)+name+">";
			}

			IPointerType type4=type as IPointerType;
			if(type4!=null) {
				return new TypeRef(type4.ElementType).GetNameWithRef(fullname)+"*";
			}

			IReferenceType type3=type as IReferenceType;
			if(type3!=null) {
				return new TypeRef(type3.ElementType).GetNameWithRef(fullname)+"%";
			}

			IOptionalModifier modifier2=type as IOptionalModifier;
			if(modifier2!=null) {
				return Modify(new TypeRef(modifier2.Modifier),new TypeRef(modifier2.ElementType),fullname,false);
			}

			IRequiredModifier modifier=type as IRequiredModifier;
			if(modifier!=null) {
				return Modify(new TypeRef(modifier.Modifier),new TypeRef(modifier.ElementType),fullname,true);
			}

			IGenericParameter parameter=type as IGenericParameter;
			if(parameter!=null) {
				return parameter.Name;
			}

			IGenericArgument argument=type as IGenericArgument;
			if(argument!=null) {
				return new TypeRef(argument.Resolve()).GetNameWithRef(fullname);
			}

			IFunctionPointer fptr=type as IFunctionPointer;
			if(fptr!=null) {
				System.Text.StringBuilder build=new System.Text.StringBuilder();
				build.Append("(");
				build.Append(new TypeRef(fptr.ReturnType.Type).GetNameWithRef(fullname));
				build.Append(" (*)(");
				bool first=true;
				foreach(IParameterDeclaration declaration in fptr.Parameters) {
					if(first)first=false;else build.Append(", ");
					build.Append(new TypeRef(declaration.ParameterType).GetNameWithRef(fullname));
				}
				build.Append("))");
				return build.ToString();
			}

			if(type is IValueTypeConstraint) {
				return "value class";
			}else if(type is IDefaultConstructorConstraint) {
				return "<DefaultConstructorConstraint>";
			}

			return "<�s���Ȍ^>";
		}
		#endregion
	}
}