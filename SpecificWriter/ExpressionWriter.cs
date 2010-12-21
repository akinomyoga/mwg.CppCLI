using Reflector.CodeModel;
using LanguageWriter=Reflector.Languages.CppCliLanguage.LanguageWriter;
using Gen=System.Collections.Generic;

namespace mwg.Reflector.CppCli{
	public static class ExpressionWriter{
		static ExpressionWriter(){
			InitializeExpressionType();
			InitializeOperatorPrecedence();
		}

		#region ExpressionType mapping
		internal enum ExpressionType{
			AddressDereference,
			AddressOf,
			AddressOut,
			AddressReference,
			AnonymousMethod,
			ArgumentList,
			ArgumentReference,
			ArrayCreate,
			ArrayIndexer,
			Assign,
			BaseReference,
			Binary,
			Block,
			CanCast,
			Cast,
			Condition,
			DelegateCreate,
			DelegateInvoke,
			EventReference,
			FieldOf,
			FieldReference,
			GenericDefault,
			Lambda,
			Literal,
			MemberInitializer,
			MethodInvoke,
			MethodOf,
			MethodReference,
			NullCoalescing,
			ObjectCreate,
			PropertyIndexer,
			PropertyReference,
			Query,
			SizeOf,
			Snippet,
			StackAlloc,
			ThisReference,
			TryCast,
			TypedReferenceCreate,
			TypeOf,
			TypeOfTypedReference,
			TypeReference,
			Unary,
			ValueOfTypedReference,
			VariableDeclaration,
			VariableReference,

			Unknown,
		}
		private static Gen::Dictionary<System.Type,ExpressionType> exp_types=new Gen::Dictionary<System.Type,ExpressionType>();
		private static void InitializeExpressionType(){
			exp_types.Add(typeof(IAddressDereferenceExpression),ExpressionType.AddressDereference);
			exp_types.Add(typeof(IAddressOfExpression),ExpressionType.AddressOf);
			exp_types.Add(typeof(IAddressOutExpression),ExpressionType.AddressOut);
			exp_types.Add(typeof(IAddressReferenceExpression),ExpressionType.AddressReference);
			exp_types.Add(typeof(IAnonymousMethodExpression),ExpressionType.AnonymousMethod);
			exp_types.Add(typeof(IArgumentListExpression),ExpressionType.ArgumentList);
			exp_types.Add(typeof(IArgumentReferenceExpression),ExpressionType.ArgumentReference);
			exp_types.Add(typeof(IArrayCreateExpression),ExpressionType.ArrayCreate);
			exp_types.Add(typeof(IArrayIndexerExpression),ExpressionType.ArrayIndexer);
			exp_types.Add(typeof(IAssignExpression),ExpressionType.Assign);
			exp_types.Add(typeof(IBaseReferenceExpression),ExpressionType.BaseReference);
			exp_types.Add(typeof(IBinaryExpression),ExpressionType.Binary);
			exp_types.Add(typeof(IBlockExpression),ExpressionType.Block);
			exp_types.Add(typeof(ICanCastExpression),ExpressionType.CanCast);
			exp_types.Add(typeof(ICastExpression),ExpressionType.Cast);
			exp_types.Add(typeof(IConditionExpression),ExpressionType.Condition);
			exp_types.Add(typeof(IDelegateCreateExpression),ExpressionType.DelegateCreate);
			exp_types.Add(typeof(IDelegateInvokeExpression),ExpressionType.DelegateInvoke);
			exp_types.Add(typeof(IEventReferenceExpression),ExpressionType.EventReference);
			exp_types.Add(typeof(IFieldOfExpression),ExpressionType.FieldOf);
			exp_types.Add(typeof(IFieldReferenceExpression),ExpressionType.FieldReference);
			exp_types.Add(typeof(IGenericDefaultExpression),ExpressionType.GenericDefault);
			exp_types.Add(typeof(ILambdaExpression),ExpressionType.Lambda);
			exp_types.Add(typeof(ILiteralExpression),ExpressionType.Literal);
			exp_types.Add(typeof(IMemberInitializerExpression),ExpressionType.MemberInitializer);
			exp_types.Add(typeof(IMethodInvokeExpression),ExpressionType.MethodInvoke);
			exp_types.Add(typeof(IMethodOfExpression),ExpressionType.MethodOf);
			exp_types.Add(typeof(IMethodReferenceExpression),ExpressionType.MethodReference);
			exp_types.Add(typeof(INullCoalescingExpression),ExpressionType.NullCoalescing);
			exp_types.Add(typeof(IObjectCreateExpression),ExpressionType.ObjectCreate);
			exp_types.Add(typeof(IPropertyIndexerExpression),ExpressionType.PropertyIndexer);
			exp_types.Add(typeof(IPropertyReferenceExpression),ExpressionType.PropertyReference);
			exp_types.Add(typeof(IQueryExpression),ExpressionType.Query);
			exp_types.Add(typeof(ISizeOfExpression),ExpressionType.SizeOf);
			exp_types.Add(typeof(ISnippetExpression),ExpressionType.Snippet);
			exp_types.Add(typeof(IStackAllocateExpression),ExpressionType.StackAlloc);
			exp_types.Add(typeof(IThisReferenceExpression),ExpressionType.ThisReference);
			exp_types.Add(typeof(ITryCastExpression),ExpressionType.TryCast);
			exp_types.Add(typeof(ITypedReferenceCreateExpression),ExpressionType.TypedReferenceCreate);
			exp_types.Add(typeof(ITypeOfExpression),ExpressionType.TypeOf);
			exp_types.Add(typeof(ITypeOfTypedReferenceExpression),ExpressionType.TypeOfTypedReference);
			exp_types.Add(typeof(ITypeReferenceExpression),ExpressionType.TypeReference);
			exp_types.Add(typeof(IUnaryExpression),ExpressionType.Unary);
			exp_types.Add(typeof(IValueOfTypedReferenceExpression),ExpressionType.ValueOfTypedReference);
			exp_types.Add(typeof(IVariableDeclarationExpression),ExpressionType.VariableDeclaration);
			exp_types.Add(typeof(IVariableReferenceExpression),ExpressionType.VariableReference);
		}
		internal static ExpressionType GetExpressionType(IExpression expression){
			System.Type t=expression.GetType();
			ExpressionType ret;
			if(exp_types.TryGetValue(expression.GetType(),out ret)){
				return ret;
			}

			System.Type[] interfaces=t.GetInterfaces();
			for(int i=0,iM=interfaces.Length;i<iM;i++){
				if(exp_types.TryGetValue(interfaces[i],out ret)){
					exp_types[t]=ret; // 覚えて置く
					return ret;
				}
			}

			return ExpressionType.Unknown;
		}
		#endregion

		#region ExpressionPrecedence
		private const int PREC_POST=13;
		private const int PREC_MEM_ACCESS=13;
		private const int PREC_PRE=12;
		private const int PREC_CAST=12;
		private const int PREC_BI_MUL=10;
		private const int PREC_COND=0;
		private static Gen::Dictionary<BinaryOperator,int> prec_binop
			=new System.Collections.Generic.Dictionary<BinaryOperator,int>();
		private static Gen::Dictionary<ExpressionType,int> prec_exp
			=new System.Collections.Generic.Dictionary<ExpressionType,int>();
		private static Gen::Dictionary<UnaryOperator,int> prec_unary
			=new System.Collections.Generic.Dictionary<UnaryOperator,int>();
		private static void InitializeOperatorPrecedence(){
			prec_exp.Add(ExpressionType.ThisReference,		15);
			prec_exp.Add(ExpressionType.ArgumentReference,	15);
			prec_exp.Add(ExpressionType.VariableReference,	15);
			prec_exp.Add(ExpressionType.Literal,			15);
			prec_exp.Add(ExpressionType.Block,				15);
			prec_exp.Add(ExpressionType.TryCast,			15);
			prec_exp.Add(ExpressionType.SizeOf,				15); // sizeof(<typename>) のみ (リファレンスでは 12 と同じだが)

			// :: 系統
			prec_exp.Add(ExpressionType.BaseReference,14);
			prec_exp.Add(ExpressionType.TypeOf,14);

			// 後置演算子の類
			prec_exp.Add(ExpressionType.EventReference,		PREC_MEM_ACCESS); 
			prec_exp.Add(ExpressionType.FieldReference,		PREC_MEM_ACCESS);
			prec_exp.Add(ExpressionType.MethodReference,	PREC_MEM_ACCESS);
			prec_exp.Add(ExpressionType.PropertyReference,	PREC_MEM_ACCESS);
			prec_exp.Add(ExpressionType.ArrayIndexer,		PREC_POST);
			prec_exp.Add(ExpressionType.PropertyIndexer,	PREC_POST);
			prec_exp.Add(ExpressionType.MethodInvoke,		PREC_POST);
			prec_unary.Add(UnaryOperator.PostDecrement,		PREC_POST);
			prec_unary.Add(UnaryOperator.PostIncrement,		PREC_POST);

			// 前置演算子の類
			prec_exp.Add(ExpressionType.ObjectCreate,		12); // gcnew / typename() の二つの場合
			prec_exp.Add(ExpressionType.AddressDereference,	PREC_PRE); // *
			prec_exp.Add(ExpressionType.AddressOf,			PREC_PRE); // &
			prec_exp.Add(ExpressionType.Cast,				PREC_CAST); // (typename)
			prec_exp.Add(ExpressionType.ArrayCreate,		PREC_PRE); // gcnew
			prec_unary.Add(UnaryOperator.PreDecrement,		PREC_PRE); // --
			prec_unary.Add(UnaryOperator.PreIncrement,		PREC_PRE); // ++
			prec_unary.Add(UnaryOperator.Negate,			PREC_PRE); // -
			prec_unary.Add(UnaryOperator.BooleanNot,		PREC_PRE); // !
			prec_unary.Add(UnaryOperator.BitwiseNot,		PREC_PRE); // ~

			prec_binop.Add(BinaryOperator.Divide,			PREC_BI_MUL);
			prec_binop.Add(BinaryOperator.Modulus,			PREC_BI_MUL);
			prec_binop.Add(BinaryOperator.Multiply,			PREC_BI_MUL);

			prec_binop.Add(BinaryOperator.Subtract,9);
			prec_binop.Add(BinaryOperator.Add,9);

			prec_binop.Add(BinaryOperator.ShiftLeft,8);
			prec_binop.Add(BinaryOperator.ShiftRight,8);

			prec_binop.Add(BinaryOperator.GreaterThan,7);
			prec_binop.Add(BinaryOperator.GreaterThanOrEqual,7);
			prec_binop.Add(BinaryOperator.LessThan,7);
			prec_binop.Add(BinaryOperator.LessThanOrEqual,7);

			prec_binop.Add(BinaryOperator.IdentityEquality,6);
			prec_binop.Add(BinaryOperator.IdentityInequality,6);
			prec_binop.Add(BinaryOperator.ValueEquality,6);
			prec_binop.Add(BinaryOperator.ValueInequality,6);
			prec_exp.Add(ExpressionType.CanCast,6);

			prec_binop.Add(BinaryOperator.BitwiseAnd,5);
			prec_binop.Add(BinaryOperator.BitwiseExclusiveOr,4);
			prec_binop.Add(BinaryOperator.BitwiseOr,3);

			prec_binop.Add(BinaryOperator.BooleanAnd,2);
			prec_binop.Add(BinaryOperator.BooleanOr,1);

			prec_exp.Add(ExpressionType.Condition,PREC_COND); // ==0;

			prec_exp.Add(ExpressionType.Assign,-1);
		}
		private static int GetExpressionPrecedence(IExpression exp){
			ExpressionType xtype=GetExpressionType(exp);
			int ret;
			if(prec_exp.TryGetValue(xtype,out ret)){
				return ret;
			}

			if(xtype==ExpressionType.Binary&&prec_binop.TryGetValue(((IBinaryExpression)exp).Operator,out ret)){
				return ret;
			}else if(xtype==ExpressionType.Unary&&prec_unary.TryGetValue(((IUnaryExpression)exp).Operator,out ret)){
				return ret;
			}

			return -2;
		}
		#endregion

		public static void WriteExpression(LanguageWriter w,IExpression exp,bool paren){
			if(paren)w.Write("(");
			switch(GetExpressionType(exp)){
				case ExpressionType.AddressDereference:
					WriteAddressDereference(w,(IAddressDereferenceExpression)exp);
					break;
				case ExpressionType.AddressOf:
					w.Write("&");
					WriteExpression(w,((IAddressOfExpression)exp).Expression,false);
					break;
				case ExpressionType.AddressOut:			// 引数 out 渡し
					WriteExpression(w,((IAddressOutExpression)exp).Expression,false);
					break;
				case ExpressionType.AddressReference:	// 引数 ref 渡し
					WriteExpression(w,((IAddressReferenceExpression)exp).Expression,false);
					break;
				case ExpressionType.ArgumentList:
					w.WriteKeyword("__arglist");
					break;
				case ExpressionType.ArgumentReference:
					w.WriteParameterReference(((IArgumentReferenceExpression)exp).Parameter);
					break;
				case ExpressionType.ArrayCreate:
					WriteArrayCreate(w,(IArrayCreateExpression)exp);
					break;
				case ExpressionType.ArrayIndexer:
					WriteArrayIndexerExpression(w,(IArrayIndexerExpression)exp);
					break;
				case ExpressionType.Assign:
					WriteAssign(w,(IAssignExpression)exp);
					break;
				case ExpressionType.BaseReference:
					w.baseType.WriteName(w);
					break;
				case ExpressionType.Binary:
					WriteBinary(w,(IBinaryExpression)exp);
					break;
				case ExpressionType.Block:
					WriteBlock(w,(IBlockExpression)exp);
					break;
				case ExpressionType.CanCast:
					WriteCanCast(w,(ICanCastExpression)exp);
					break;
				case ExpressionType.Cast:
					WriteCast(w,(ICastExpression)exp);
					break;
				case ExpressionType.Condition:
					WriteCondition(w,(IConditionExpression)exp);
					break;
				case ExpressionType.DelegateCreate:
					WriteDelegateCreate(w,(IDelegateCreateExpression)exp);
					break;
				case ExpressionType.Literal:
					w.WriteAsLiteral(((ILiteralExpression)exp).Value);
					break;
				case ExpressionType.MethodInvoke:
					WriteMethodInvoke(w,(IMethodInvokeExpression)exp);
					break;
				case ExpressionType.ObjectCreate:
					WriteObjectCreate(w,(IObjectCreateExpression)exp);
					break;
				case ExpressionType.SizeOf:
					w.WriteKeyword("sizeof");
					w.Write("(");
					new TypeRef(((ISizeOfExpression)exp).Type).WriteName(w);
					w.Write(")");
					break;
				case ExpressionType.Snippet:
					w.WriteAsLiteral(((ISnippetExpression)exp).Value);
					break;
				case ExpressionType.ThisReference:
					w.WriteKeyword("this");
					break;
				case ExpressionType.TryCast:
					WriteTryCast(w,(ITryCastExpression)exp);
					break;
				case ExpressionType.TypeOf:
					new TypeRef(((ITypeOfExpression)exp).Type).WriteName(w);
					w.Write("::");
					w.WriteKeyword("typeid");
					break;
				case ExpressionType.TypeReference:
					WriteTypeReference(w,(ITypeReferenceExpression)exp);
					break;
				case ExpressionType.Unary:
					WriteUnary(w,(IUnaryExpression)exp);
					break;
				case ExpressionType.VariableDeclaration:
					WriteVariableDeclaration(w,((IVariableDeclarationExpression)exp).Variable);
					break;
				case ExpressionType.VariableReference:
					w.WriteVariableReference(((IVariableReferenceExpression)exp).Variable);
					break;
				case ExpressionType.MemberInitializer: // 属性の初期化の際のメンバ指定
					WriteMemberInitializer(w,(IMemberInitializerExpression)exp);
					break;
				//---------------------------------------------------
				// メンバアクセス
				//---------------------------------------------------
				case ExpressionType.EventReference:
					WriteEventReference(w,(IEventReferenceExpression)exp);
					break;
				case ExpressionType.FieldReference:
					IFieldReferenceExpression exp_fld=(IFieldReferenceExpression)exp;
					WriteMemberAccess(w,exp_fld.Target);
					w.WriteFieldReference(exp_fld.Field);
					break;
				case ExpressionType.PropertyReference:
					WritePropertyReference(w,(IPropertyReferenceExpression)exp);
					break;
				case ExpressionType.PropertyIndexer:
					IPropertyIndexerExpression exp_pind=(IPropertyIndexerExpression)exp;
					WritePropertyReference(w,exp_pind.Target);
					w.Write("[");
					w.WriteExpressionCollection(exp_pind.Indices);
					w.Write("]");
					break;
				case ExpressionType.MethodReference:
					IMethodReferenceExpression exp_meth=(IMethodReferenceExpression)exp;
					WriteMemberAccess(w,exp_meth.Target);
					w.WriteMethodReference(exp_meth.Method);
					break;
				//---------------------------------------------------
				//	代替
				//---------------------------------------------------
				case ExpressionType.StackAlloc:
					WriteStackAllocate(w,(IStackAllocateExpression)exp);
					break;
				case ExpressionType.AnonymousMethod:
					WriteAnonymousMethod(w,(IAnonymousMethodExpression)exp);
					break;
				//---------------------------------------------------
				//	以下未対応
				//---------------------------------------------------
				case ExpressionType.NullCoalescing:
					WriteBinaryNullCoalescing(w,(INullCoalescingExpression)exp);
					break;
				case ExpressionType.DelegateInvoke:
				case ExpressionType.FieldOf:
				case ExpressionType.GenericDefault:
				case ExpressionType.Lambda:
				case ExpressionType.MethodOf:
				case ExpressionType.Query:
					goto default;
				case ExpressionType.TypedReferenceCreate:
				case ExpressionType.TypeOfTypedReference:
				case ExpressionType.ValueOfTypedReference:
					//throw new System.NotImplementedException("未だ実装していません\r\n");
					goto default;
				case ExpressionType.Unknown:
				default:
					ThrowUnknownExpression(exp);
					break;
			}
			if(paren)w.Write(")");
		}

		private static void ThrowUnknownExpression(IExpression exp){
			System.Text.StringBuilder build=new System.Text.StringBuilder();
			build.Append("未知の Expression です。\r\n");
			build.Append("----------------------------\r\n型\r\n");
			build.Append(exp.GetType());
			build.Append("\r\n----------------------------\r\nインターフェイス\r\n");
			foreach(System.Type t in exp.GetType().GetInterfaces())
				build.Append(t);
			throw new System.Exception(build.ToString());
		}

		private static void WriteAddressDereference(LanguageWriter w,IAddressDereferenceExpression exp){
			w.Write("*");
			WriteExpression(w,exp.Expression,PREC_PRE>GetExpressionPrecedence(exp.Expression));
		}
		private static void WriteArrayCreate(LanguageWriter w,IArrayCreateExpression exp){
			w.WriteKeyword("gcnew");
			w.Write(" ");
			w.WriteKeyword("array");
			w.Write("<");
			
			new TypeRef(exp.Type).WriteNameWithRef(w);
			if(exp.Dimensions!=null&&exp.Dimensions.Count>1){
				w.Write(", ");
				w.WriteAsLiteral(exp.Dimensions.Count);
			}
			w.Write(">");

			// 要素数の指定
			if(exp.Dimensions!=null){
				w.Write("(");
				w.WriteExpressionCollection(exp.Dimensions);
				w.Write(")");
			}

			// {a, b, ... 要素の指定}
			IBlockExpression initializer=exp.Initializer;
			if(initializer!=null){
				WriteBlock(w,initializer);
			}
		}
		private static void WriteArrayIndexerExpression(LanguageWriter w,IArrayIndexerExpression exp){
			w.WriteExpression(exp.Target);
			w.Write("[");
			w.WriteExpressionCollection(exp.Indices);
			w.Write("]");
		}
		private static void WriteBlock(LanguageWriter w,IBlockExpression exp){
			w.Write("{");
			w.WriteExpressionCollection(((IBlockExpression)exp).Expressions);
			w.Write("}");
		}
		private static void WriteAssign(LanguageWriter w,IAssignExpression exp){
#if EXTRA_TEMP
			// 無駄な変数の除去
			//   之はその内に StatementAnalyze に引っ越した方が分かりやすいかも知れない
			//   但し、その場合には式内で代入を行う場合には対応出来ない (例: (i=j)==0 等)
			IVariableDeclarationExpression target=exp.Target as IVariableDeclarationExpression;
			IVariableReferenceExpression target2=exp.Target as IVariableReferenceExpression;
			IVariableReferenceExpression right=exp.Expression as IVariableReferenceExpression;
			if(w.ExtraTemporaries!=null){

				// 無駄変数の宣言
				if(target!=null)for(int i=0;i<w.ExtraTemporaries.Length;i++){
					if(!w.EssentialTemporaries[i]&&target.Variable.Name==w.ExtraTemporaries[i]) {
						return;
					}
				}

				// 無駄変数から無駄変数への受け渡し
				if(target2!=null&&right!=null){
					int iTarget=-1;
					int iRight=-1;
					for(int j=0;j<w.ExtraTemporaries.Length;j++) {
						if(target2.Variable.ToString()==w.ExtraTemporaries[j]){
							iTarget=j;
							w.VerifyCorrectBlock(j);
						}
						if(right.Variable.ToString()==w.ExtraTemporaries[j]){
							iRight=j;
							w.VerifyCorrectBlock(j);
						}
					}
					if(iTarget>=0&&!w.EssentialTemporaries[iTarget]){
						string str;
						if(iRight>=0&&!w.EssentialTemporaries[iRight]){
							if(w.ExtraMappings[iRight]==null){
								w.EssentialTemporaries[iRight]=true;
								str=null;
							}else{
								str=w.ExtraMappings[iRight];
							}
						}else{
							str=right.Variable.ToString();
						}
						if(str!=null&&target2.Variable.ToString()[0]==str[0]) {
							w.ExtraMappings[iTarget]=str;
							return;
						}
					}
				}
			}
#endif
			WriteExpression(w,exp.Target,false);
			w.Write(" = ");
			WriteExpression(w,exp.Expression,false);
		}

		#region Write 二項演算子の類
		private static void WriteBinary(LanguageWriter w,IBinaryExpression exp){
			WriteExpression(w,exp.Left,prec_binop[exp.Operator]>GetExpressionPrecedence(exp.Left));

			w.Write(" ");
			WriteBinaryOperator(w,exp.Operator);
			w.Write(" ");

			WriteExpression(w,exp.Right,prec_binop[exp.Operator]>=GetExpressionPrecedence(exp.Right));
		}
		private static void WriteBinaryNullCoalescing(LanguageWriter w,INullCoalescingExpression exp){
			const int PREC_NULL_COAL=0;
			WriteExpression(w,exp.Condition,PREC_NULL_COAL>GetExpressionPrecedence(exp.Condition));

			w.Write(" ?? ");

			WriteExpression(w,exp.Expression,PREC_NULL_COAL>=GetExpressionPrecedence(exp.Expression));
		}

		private static void WriteBinaryOperator(LanguageWriter w,BinaryOperator binaryOperator) {
			switch(binaryOperator) {
				case BinaryOperator.Add:
					w.Write("+");
					break;
				case BinaryOperator.Subtract:
					w.Write("-");
					break;
				case BinaryOperator.Multiply:
					w.Write("*");
					break;
				case BinaryOperator.Divide:
					w.Write("/");
					break;
				case BinaryOperator.Modulus:
					w.Write("%");
					break;
				case BinaryOperator.ShiftLeft:
					w.Write("<<");
					break;
				case BinaryOperator.ShiftRight:
					w.Write(">>");
					break;
				case BinaryOperator.IdentityEquality:
					w.Write("==");
					break;
				case BinaryOperator.IdentityInequality:
					w.Write("!=");
					break;
				case BinaryOperator.ValueEquality:
					w.Write("==");
					break;
				case BinaryOperator.ValueInequality:
					w.Write("!=");
					break;
				case BinaryOperator.BitwiseOr:
					w.Write("|");
					break;
				case BinaryOperator.BitwiseAnd:
					w.Write("&");
					break;
				case BinaryOperator.BitwiseExclusiveOr:
					w.Write("^");
					break;
				case BinaryOperator.BooleanOr:
					w.Write("||");
					break;
				case BinaryOperator.BooleanAnd:
					w.Write("&&");
					break;
				case BinaryOperator.LessThan:
					w.Write("<");
					break;
				case BinaryOperator.LessThanOrEqual:
					w.Write("<=");
					break;
				case BinaryOperator.GreaterThan:
					w.Write(">");
					break;
				case BinaryOperator.GreaterThanOrEqual:
					w.Write(">=");
					break;
				default:
					throw new System.NotSupportedException(binaryOperator.ToString());
			}
		}
		#endregion

		private static void WriteCanCast(LanguageWriter w,ICanCastExpression exp) {
			w.WriteKeyword("dynamic_cast");
			w.Write("<");
			new TypeRef(exp.TargetType).WriteNameWithRef(w);
			w.Write(">");
			w.Write("(");
			WriteExpression(w,exp.Expression,false);
			w.Write(")");
			w.Write(" != ");
			w.WriteLiteral("nullptr");
		}
		private static void WriteCast(LanguageWriter w,ICastExpression exp){
			TypeRef type=new TypeRef(exp.TargetType);
			bool paren=!type.IsFunctionPointer;

			if(paren)w.Write("(");
			type.WriteNameWithRef(w);
			if(paren)w.Write(")");

			WriteExpression(w,exp.Expression,PREC_CAST>GetExpressionPrecedence(exp.Expression));
		}
		private static void WriteCondition(LanguageWriter w,IConditionExpression exp){
			WriteExpression(w,exp.Condition,PREC_COND>GetExpressionPrecedence(exp.Condition));
			w.Write(" ? ");
			WriteExpression(w,exp.Then,false);
			w.Write(" : ");
			WriteExpression(w,exp.Else,false);
		}
		private static void WriteMemberAccess(LanguageWriter w,IExpression target){
			if(target==null)return;

			bool isref=true;
			ExpressionType xtype=GetExpressionType(target);
			TypeRef type;
			switch(xtype){
				case ExpressionType.TypeReference:
					WriteTypeReference(w,(ITypeReferenceExpression)target);
					w.Write("::");
					return;
				case ExpressionType.BaseReference:
					w.baseType.WriteName(w);
					w.Write("::");
					return;
				case ExpressionType.VariableReference:
					IVariableReference var_ref=((IVariableReferenceExpression)target).Variable;
					if(var_ref==null)break;
					IVariableDeclaration var_decl=var_ref.Resolve();
					if(var_decl==null)break;
					if(w.scope[var_decl.Name].local_scope){
						isref=false;
						break;
					}
					type=new TypeRef(var_decl.VariableType);
					goto fromtype;
				default:
					type=TypeRef.GetReturnType(target);
					goto fromtype;
				fromtype:
//#warning Modifier や % や & が付いている場合に関しては未だ
					isref=type.IsEmpty||!type.IsValueType&&!type.IsPrimitive;
					break;
			}


			WriteExpression(w,target,PREC_MEM_ACCESS>GetExpressionPrecedence(target));
			w.Write(isref?"->":".");
		}
		internal static void WriteEventReference(LanguageWriter w,IEventReferenceExpression exp){
			WriteMemberAccess(w,exp.Target);
			w.WriteEventReference(exp.Event);
		}
		private static void WritePropertyReference(LanguageWriter w,IPropertyReferenceExpression exp){
			WriteMemberAccess(w,exp.Target);
			w.WritePropertyReference(exp.Property);
		}
		private static void WriteMethodInvoke(LanguageWriter w,IMethodInvokeExpression exp) {
			WriteExpression(w,exp.Method,PREC_MEM_ACCESS>GetExpressionPrecedence(exp.Method));
			w.Write("(");
			w.WriteExpressionCollection(exp.Arguments);
			w.Write(")");
		}
		private static void WriteMemberInitializer(LanguageWriter w,IMemberInitializerExpression exp) {
			w.WriteMemberReference(exp.Member);
			w.Write(" = ");
			WriteExpression(w,exp.Value,false);
		}
		private static void WriteObjectCreate(LanguageWriter w,IObjectCreateExpression exp) {
			if(exp.Constructor==null){
				// 構造体のデフォルトコンストラクタ (StatementAnalyze でフィルタリングされている筈だけれども)
				new TypeRef(exp.Type).WriteName(w);
#if FALSE
				w.Write("(");
#warning 構造体デフォルトコンストラクタ 対応漏れがあるかもしれないので保留
				w.WriteComment("/* C++/CLI では本来は T value; 等と書く筈です。*/");
				w.Write(") ");
#else
				w.Write("()");
#endif
			}else{
				ITypeReference declaringType=exp.Constructor.DeclaringType as ITypeReference;
				TypeRef decl_type=new TypeRef(exp.Constructor.DeclaringType);
				if(decl_type.IsRefType){
					w.WriteKeyword("gcnew");
					w.Write(" ");
				}
				decl_type.WriteName(w);

				w.Write("(");
				w.WriteExpressionCollection(exp.Arguments);
				w.Write(")");
			}

			if(exp.Initializer!=null)
				WriteBlock(w,exp.Initializer);

		}
		private static void WriteTryCast(LanguageWriter w,ITryCastExpression exp) {
			w.WriteKeyword("dynamic_cast");
			w.Write("<");
			new TypeRef(exp.TargetType).WriteNameWithRef(w);
			w.Write(">(");
			WriteExpression(w,exp.Expression,false);
			w.Write(")");
		}
		private static void WriteTypeReference(LanguageWriter w,ITypeReferenceExpression exp){
			// if(exp.Type.Name=="UnmanagedType")return; // ←何故 UnmanagedType を略すのか不明
			if(exp.Type.Name=="<Module>")return;
			new TypeRef(exp.Type).WriteName(w);
		}
		private static void WriteTypeReference(LanguageWriter w,ITypeReference type){
			if(type.Name=="<Module>")return;
			new TypeRef(type).WriteName(w);
		}
		private static void WriteUnary(LanguageWriter w,IUnaryExpression exp) {
			bool post=false;
			string opName;
			switch(exp.Operator){
				case UnaryOperator.Negate:
					opName="-";
					break;
				case UnaryOperator.BooleanNot:
					opName="!";
					break;
				case UnaryOperator.BitwiseNot:
					opName="~";
					break;
				case UnaryOperator.PreIncrement:
					opName="++";
					break;
				case UnaryOperator.PreDecrement:
					opName="--";
					break;
				case UnaryOperator.PostIncrement:
					opName="++";
					post=true;
					break;
				case UnaryOperator.PostDecrement:
					opName="--";
					post=true;
					break;
				default:
					throw new System.NotSupportedException(
						"指定した種類の単項演算には対応していません: "+exp.Operator.ToString()
						);
			}

			// 書込
			if(post){
				WriteExpression(w,exp.Expression,PREC_POST>GetExpressionPrecedence(exp.Expression));
				w.Write(opName);
			}else{
				w.Write(opName);
				WriteExpression(w,exp.Expression,PREC_PRE>GetExpressionPrecedence(exp.Expression));
			}
		}
		internal static void WriteVariableDeclaration(LanguageWriter w,IVariableDeclaration var_decl) {
			// local 変数の登録
			w.scope.RegisterVariable(var_decl,false);

#warning pinned の情報なども入れる様にする
			new TypeRef(var_decl.VariableType).WriteNameWithRef(w);
			w.Write(" ");
			w.WriteDeclaration(w.scope[var_decl.Name].disp_name);
		}
		//===========================================================
		//		追加
		//===========================================================
		private static void WriteStackAllocate(LanguageWriter w,IStackAllocateExpression exp){
			TypeRef type=new TypeRef(exp.Type);
			w.Write("(");
			type.WriteNameWithRef(w);
			w.Write("*)::");
			w.WriteReference("_alloca","Crt 関数 #include <malloc.h>",null);
			w.Write("(");
			w.WriteKeyword("sizeof");
			w.Write("(");
			type.WriteName(w);
			w.Write(")*");
			WriteExpression(w,exp.Expression,PREC_BI_MUL>GetExpressionPrecedence(exp.Expression));
			w.Write(")");
		}
		private static void WriteDelegateCreate(LanguageWriter w,IDelegateCreateExpression exp){
			w.WriteKeyword("gcnew");
			w.Write(" ");
			new TypeRef(exp.DelegateType).WriteName(w);
			w.Write("(");
			WriteExpression(w,exp.Target,false);
			w.Write(", &");
			WriteTypeReference(w,(ITypeReference)exp.Method.DeclaringType);
			w.Write("::");
			w.WriteMethodReference(exp.Method);
			w.Write(")");
		}
		// 匿名メソッドは、ラムダ式文法で代替する事にする。
		// 実際には、変数のキャプチャなどを行うと C++/CLI ではコンパイル出来ない。
		private static void WriteAnonymousMethod(LanguageWriter w,IAnonymousMethodExpression exp){
			w.Write("[&]");
			w.WriteMethodParameterCollection(exp.Parameters);

			// 戻り型
			TypeRef return_type=new TypeRef(exp.ReturnType.Type);
			if(!return_type.IsVoid){
				w.Write(" -> ");
				return_type.WriteNameWithRef(w);
			}

			// 中身
			w.PushScope();
			w.Write(" {");
			w.WriteLine();
			w.WriteIndent();
			w.WriteStatement(exp.Body);
			w.WriteOutdent();
			w.Write("}");
			w.PopScope();
		}
		//===========================================================
		//		代替 (未対応)
		//===========================================================
	}
}