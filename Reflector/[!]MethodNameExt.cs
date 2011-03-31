namespace Reflector.Languages {
	using System;
	using System.Collections;
	using Gen=System.Collections.Generic;
	using System.Runtime.InteropServices;

	[System.Obsolete]
	internal class MethodNameExt {
		private static Gen::Dictionary<string,string> specialMethodNames
			=new System.Collections.Generic.Dictionary<string,string>();
		static MethodNameExt(){
			specialMethodNames["op_AddressOf"]="operator&";
			specialMethodNames["op_OnesComplement"]="operator~";
			specialMethodNames["op_PointerDereference"]="operator*";
			specialMethodNames["op_UnaryNegation"]="operator-";
			specialMethodNames["op_UnaryPlus"]="operator+";
			specialMethodNames["op_Increment"]="operator++";
			specialMethodNames["op_Decrement"]="operator--";

			specialMethodNames["op_Addition"]="operator+";
			specialMethodNames["op_Subtraction"]="operator-";
			specialMethodNames["op_Multiply"]="operator*";
			specialMethodNames["op_Division"]="operator/";
			specialMethodNames["op_Modulus"]="operator%";

			specialMethodNames["op_BitwiseAnd"]="operator&";
			specialMethodNames["op_BitwiseOr"]="operator|";
			specialMethodNames["op_ExclusiveOr"]="operator^";
			specialMethodNames["op_LeftShift"]="operator<<";
			specialMethodNames["op_RightShift"]="operator>>";

			specialMethodNames["op_Equality"]="operator==";
			specialMethodNames["op_Inequality"]="operator!=";
			specialMethodNames["op_GreaterThanOrEqual"]="operator>=";
			specialMethodNames["op_LessThanOrEqual"]="operator<=";
			specialMethodNames["op_GreaterThan"]="operator>";
			specialMethodNames["op_LessThan"]="operator<";

			specialMethodNames["op_Negation"]="operator!";

			specialMethodNames["op_LogicalAnd"]="operator&&"; // 追加
			specialMethodNames["op_LogicalOr"]="operator||"; // 追加
			specialMethodNames["op_LogicalNot"]="operator!";
			specialMethodNames["op_True"]="operator true";
			specialMethodNames["op_False"]="operator false";

			specialMethodNames["op_MemberSelection"]="operator ->";
			specialMethodNames["op_PointerToMemberSelection"]="operator->*";
			specialMethodNames["op_Implicit"]="implicit ";
			specialMethodNames["op_Explicit"]="explicit ";

			// 追加
			specialMethodNames["op_Comma"]="operator,";
			specialMethodNames["op_FunctionCall"]="operator()";
			specialMethodNames["op_Subscript"]="operator[]";
			specialMethodNames["op_Assign"]="operator=";

			specialMethodNames["op_AdditionAssignment"]="operator+=";
			specialMethodNames["op_SubtractionAssignment"]="operator-=";
			specialMethodNames["op_MultiplyAssignment"]="operator*=";
			specialMethodNames["op_DivisionAssignment"]="operator/=";
			specialMethodNames["op_ModulusAssignment"]="operator%=";

			specialMethodNames["op_BitwiseAndAssignment"]="operator&=";
			specialMethodNames["op_BitwiseOrAssignment"]="operator|=";
			specialMethodNames["op_ExclusiveOrAssignment"]="operator^=";
			specialMethodNames["op_LeftShiftAssignment"]="operator<<=";
			specialMethodNames["op_RightShiftAssignment"]="operator>>=";
		}

		private bool backing_store_Constructor;
		private bool backing_store_Explicit;
		private bool backing_store_ImplicitOrExplicit;
		private bool backing_store_StaticConstructor;
		public string N;
		public ICollection O;
		public string OriginalN;

		public MethodNameExt(string Name,ICollection Overrides,string TypeName) {
			if(Name=="op_Explicit") {
				this.ImplicitOrExplicit=true;
				this.Explicit=true;
			}else if(Name=="op_Implicit"){
				this.ImplicitOrExplicit=true;
			}else if(Name==".ctor"){
				Name=TypeName;
				this.Constructor=true;
			}else if(Name==".cctor"){
				Name=TypeName;
				this.StaticConstructor=true;
			}
			this.OriginalN=Name;
			this.O=Overrides;
		}

		public bool Constructor {
			get {return this.backing_store_Constructor;}
			set {this.backing_store_Constructor=value;}
		}

		public bool Explicit {
			get {return this.backing_store_Explicit;}
			set {this.backing_store_Explicit=value;}
		}

		public bool ImplicitOrExplicit {
			get {return this.backing_store_ImplicitOrExplicit;}
			set {this.backing_store_ImplicitOrExplicit=value;}
		}

		public string Name {
			get {
				if(this.N==null) {
					this.N=this.OriginalN;
					if(!this.Constructor) {
						// 何の為にあるのか不明
						//if(this.O==null||this.O.Count>0){
						//	this.N=this.N.Replace(".","_");
						//}

						string specialName;
						if(specialMethodNames.TryGetValue(this.N,out specialName)){
							this.N=specialName;
						}
					}
				}
				return this.N;
			}
		}

		public bool StaticConstructor {
			get {return this.backing_store_StaticConstructor;}
			set {this.backing_store_StaticConstructor=value;}
		}
	}
}

