using Reflector.CodeModel;
using Gen=System.Collections.Generic;
using LanguageWriter=Reflector.Languages.CppCliLanguage.LanguageWriter;
#if EXTRA_TEMP
using NewBlock=Reflector.Languages.CppCliLanguage.LanguageWriter.NewBlock;
#endif

namespace mwg.Reflector.CppCli{
	public static class StatementWriter{

		static StatementWriter(){
			InitializeStatementType();
		}

		#region StatementType
		private enum StatementType{
			AttachEvent,
			Block,
			Break,
			Comment,
			Condition,
			Continue,
			DebugBreak,
			Do,
			Expression,
			Fixed,
			ForEach,
			For,
			Goto,
			Labeled,
			Lock,
			MemoryCopy,
			MemoryInitialize,
			MethodReturn,
			RemoveEvent,
			Switch,
			ThrowException,
			TryCatch,
			Using,
			While,
			Unknown,

			LocalRefVariable,
			Delete,
			Label,
			DefaultConstruction,
		}
		private static Gen::Dictionary<System.Type,StatementType> state_types
			=new System.Collections.Generic.Dictionary<System.Type,StatementType>();
		private static void InitializeStatementType(){
			state_types.Add(typeof(IAttachEventStatement),StatementType.AttachEvent);
			state_types.Add(typeof(IBlockStatement),StatementType.Block);
			state_types.Add(typeof(IBreakStatement),StatementType.Break);
			state_types.Add(typeof(ICommentStatement),StatementType.Comment);
			state_types.Add(typeof(IConditionStatement),StatementType.Condition);
			state_types.Add(typeof(IContinueStatement),StatementType.Continue);
			state_types.Add(typeof(IDebugBreakStatement),StatementType.DebugBreak);
			state_types.Add(typeof(IDoStatement),StatementType.Do);
			state_types.Add(typeof(IExpressionStatement),StatementType.Expression);
			state_types.Add(typeof(IFixedStatement),StatementType.Fixed);
			state_types.Add(typeof(IForEachStatement),StatementType.ForEach);
			state_types.Add(typeof(IForStatement),StatementType.For);
			state_types.Add(typeof(IGotoStatement),StatementType.Goto);
			state_types.Add(typeof(ILabeledStatement),StatementType.Labeled);
			state_types.Add(typeof(ILockStatement),StatementType.Lock);
			state_types.Add(typeof(IMemoryCopyStatement),StatementType.MemoryCopy);
			state_types.Add(typeof(IMemoryInitializeStatement),StatementType.MemoryInitialize);
			state_types.Add(typeof(IMethodReturnStatement),StatementType.MethodReturn);
			state_types.Add(typeof(IRemoveEventStatement),StatementType.RemoveEvent);
			state_types.Add(typeof(ISwitchStatement),StatementType.Switch);
			state_types.Add(typeof(IThrowExceptionStatement),StatementType.ThrowException);
			state_types.Add(typeof(ITryCatchFinallyStatement),StatementType.TryCatch);
			state_types.Add(typeof(IUsingStatement),StatementType.Using);
			state_types.Add(typeof(IWhileStatement),StatementType.While);

			// �ǉ�
			state_types.Add(typeof(LocalRefVariableStatement),StatementType.LocalRefVariable);
			state_types.Add(typeof(DeleteStatement),StatementType.Delete);
			state_types.Add(typeof(LabelStatement),StatementType.Label);
			state_types.Add(typeof(DefaultConstructionStatement),StatementType.DefaultConstruction);
		}
		private static StatementType GetExpressionType(IStatement statement){
			System.Type t=statement.GetType();
			StatementType ret;
			if(state_types.TryGetValue(statement.GetType(),out ret)){
				return ret;
			}

			System.Type[] interfaces=t.GetInterfaces();
			for(int i=0,iM=interfaces.Length;i<iM;i++){
				if(state_types.TryGetValue(interfaces[i],out ret)) {
					state_types[t]=ret; // �o���Ēu��
					return ret;
				}
			}

			return StatementType.Unknown;
		}
		#endregion

		public static void WriteStatement(LanguageWriter w,IStatement state){
			switch(GetExpressionType(state)){
				case StatementType.AttachEvent:
					WriteAttachEvent(w,(IAttachEventStatement)state);
					break;
				case StatementType.Block:
					WriteBlock(w,(IBlockStatement)state);
					break;
				case StatementType.Break:
					w.WriteKeyword("break");
					w.Write(";");
					w.WriteLine();
					break;
				case StatementType.Condition:
					WriteCondition(w,(IConditionStatement)state);
					break;
				case StatementType.Continue:
					w.WriteKeyword("continue");
					w.Write(";");
					w.WriteLine();
					break;
				case StatementType.DefaultConstruction:
					WriteDefaultConstruction(w,(DefaultConstructionStatement)state);
					break;
				case StatementType.Delete:
					w.WriteKeyword("delete");
					w.Write(" ");
					ExpressionWriter.WriteExpression(w,((DeleteStatement)state).deleteTarget,false);
					w.Write(";");
					w.WriteLine();
					break;
				case StatementType.Do:
					WriteDo(w,(IDoStatement)state);
					break;
				case StatementType.Expression:
					WriteExpression(w,(IExpressionStatement)state);
					break;
				case StatementType.Fixed:
					WriteFixed(w,(IFixedStatement)state);
					break;
				case StatementType.For:
					WriteFor(w,(IForStatement)state);
					break;
				case StatementType.ForEach:
					WriteForEach(w,(IForEachStatement)state);
					break;
				case StatementType.Goto:
					w.WriteKeyword("goto");
					w.Write(" ");
					w.Write(((IGotoStatement)state).Name);
					w.Write(";");
					w.WriteLine();
					break;
				case StatementType.Label:
					w.__WriteLabel(((LabelStatement)state).label_name);
					break;
				case StatementType.Labeled:
					WriteLabeled(w,(ILabeledStatement)state);
					break;
				case StatementType.LocalRefVariable:
					WriteLocalRefVariable(w,(LocalRefVariableStatement)state);
					break;
				case StatementType.Lock:
					WriteLock(w,(ILockStatement)state);
					break;
				case StatementType.MemoryCopy:
					WriteMemoryCopy(w,(IMemoryCopyStatement)state);
					break;
				case StatementType.MemoryInitialize:
					WriteMemoryInitialize(w,(IMemoryInitializeStatement)state);
					break;
				case StatementType.MethodReturn:
					WriteMethodReturn(w,(IMethodReturnStatement)state);
					break;
				case StatementType.RemoveEvent:
					WriteRemoveEvent(w,(IRemoveEventStatement)state);
					break;
				case StatementType.Switch:
					WriteSwitch(w,(ISwitchStatement)state);
					break;
				case StatementType.ThrowException:
					WriteThrowException(w,(IThrowExceptionStatement)state);
					break;
				case StatementType.TryCatch:
					WriteTryCatchFinally(w,(ITryCatchFinallyStatement)state);
					break;
			//	case StatementType.Using:
			//		WriteUsing(w,(IUsingStatement)state);
			//		break;
				case StatementType.While:
					WriteWhile(w,(IWhileStatement)state);
					break;
				case StatementType.Comment:
				case StatementType.DebugBreak:
				case StatementType.Unknown:
				default:
					ThrowUnknownStatement(state);
					break;
			}
			w.SkipWriteLine=false;
		}
		private static void ThrowUnknownStatement(IStatement state) {
			System.Text.StringBuilder build=new System.Text.StringBuilder();
			build.Append("���m�� Statement �ł��B\r\n");
			build.Append("----------------------------\r\n�^\r\n");
			build.Append(state.GetType());
			build.Append("\r\n----------------------------\r\n�C���^�[�t�F�C�X\r\n");
			foreach(System.Type t in state.GetType().GetInterfaces())
				build.Append(t);
			throw new System.Exception(build.ToString());
		}
		private static void WriteBracedStatement(LanguageWriter w,IStatement statement) {
			w.PushScope();

			w.Write("{");
			w.WriteLine();
			w.WriteIndent();
			WriteStatement(w,statement);
			w.WriteOutdent();
			w.Write("}");

			w.PopScope();
		}
		private static bool IsBlank(IStatement state) {
			if(state==null)return true;

			IBlockStatement statement=state as IBlockStatement;
			if(statement!=null) {
				IStatementCollection statements=statement.Statements;
				if(statements!=null){
					return statements.Count==0;
				}
			}
			return false;
		}
		private static void WriteStatementCollection(LanguageWriter w,IStatementCollection statementCollection) {
			foreach(IStatement current in StatementAnalyze.ModifyStatements(statementCollection)) {
				WriteStatement(w,current);
			}
		}
		//===========================================================
		//		�e�֐�
		//===========================================================
		private static void WriteAttachEvent(LanguageWriter w,IAttachEventStatement state) {
			ExpressionWriter.WriteEventReference(w,state.Event);
			w.Write(" += ");
			ExpressionWriter.WriteExpression(w,state.Listener,false);
			w.Write(";");
			w.WriteLine();
		}
		internal static void WriteBlock(LanguageWriter w,IBlockStatement blockStatement) {
			if(blockStatement.Statements.Count==0)return;
			WriteStatementCollection(w,blockStatement.Statements);
		}
		private static void WriteLocalRefVariable(LanguageWriter w,LocalRefVariableStatement state){
			state.Write(w);
			return;

			if(!state.noblock){
				w.PushScope();
				//---------------------------------------------
				w.Write("{");
				w.WriteLine();
				w.WriteIndent();
			}
			if(!w.SuppressOutput){
				//*
#warning local-ref: ��w�Ŗ��ʂȐ錾����菜���ׂ�
				if(w.scope.ContainsVariable(state.var_name)){
					w.scope[state.var_name].local_scope=true;
				}else{
					w.scope.RegisterVariable(state.var_name,true);
				}
				//*/
			}

			IObjectCreateExpression exp_create=state.exp as IObjectCreateExpression;
			bool nohandle=exp_create!=null; // �n���h���\�L�łȂ��Ă��ǂ����ǂ���
			if(nohandle){
				// �ϐ��^
				IOptionalModifier modopt=state.var_type as IOptionalModifier;
				if(modopt!=null&&modopt.Modifier.Namespace=="System.Runtime.CompilerServices"&&modopt.Modifier.Name=="IsConst") {
					state.var_type=modopt.ElementType;
				}
				new TypeRef(state.var_type).WriteName(w);

				// �ϐ���
				w.Write(" ");
				w.WriteDeclaration(state.var_name);

				// �������q
				if(exp_create.Arguments.Count==0) {
					w.WriteComment(" /* ����̃R���X�g���N�^ */");
				} else {
					w.Write("(");
					w.WriteExpressionCollection(exp_create.Arguments);
					w.Write(")");
				}
			}else{
				// = �ő���̏ꍇ: �n���h���\�L�łȂ��ƁA�R���X�g���N�^�Ăяo���ɂȂ��Ă��܂�

				// �ϐ��^
				new TypeRef(state.var_type).WriteNameWithRef(w);

				// �ϐ���
				w.Write(" ");
				w.WriteDeclaration(state.var_name);

				// �������l
				w.Write("=");
				ExpressionWriter.WriteExpression(w,state.exp,false);
			}

			w.Write(";");

			// ��ɑ������e
			w.WriteLine();
			WriteBlock(w,state.block);

			// �n���h���\�L�Ő錾�����ꍇ�͂����� delete
			if(!nohandle)
				WriteStatement(w,new DeleteStatement(new VariableReferenceExpression(state.decl)));

			w.WriteOutdent();
			w.Write("}");
			w.WriteLine();
			if(state.labelname!=null) {
				w.__WriteLabel(state.labelname);
				w.Write(";");
				w.WriteLine();
			}
			if(!state.noblock){
				//---------------------------------------------
				w.PopScope();
			}
			//*/
		}
		private static void WriteCondition(LanguageWriter w,IConditionStatement state) {
#if EXTRA_TEMP
			using(NewBlock block3=new NewBlock(w)) {
				w.WriteKeyword("if");
				w.Write(" ");
				w.Write("(");
				ExpressionWriter.WriteExpression(w,state.Condition,false);
				w.Write(") ");

				using(NewBlock block2=new NewBlock(w)) {
					WriteBracedStatement(w,state.Then);
					if(!IsBlank(state.Else)) {
						using(NewBlock block=new NewBlock(w)) {
							w.Write(" ");
							w.WriteKeyword("else");
							w.Write(" ");
							WriteBracedStatement(w,state.Else);
						}
					}
					w.WriteLine();
				}
			}
#else
			w.WriteKeyword("if");
			w.Write(" ");
			w.Write("(");
			ExpressionWriter.WriteExpression(w,state.Condition,false);
			w.Write(") ");

			WriteBracedStatement(w,state.Then);
			if(!IsBlank(state.Else)) {
				w.Write(" ");
				w.WriteKeyword("else");
				w.Write(" ");
				WriteBracedStatement(w,state.Else);
			}
			w.WriteLine();
#endif

		}
		private static void WriteDefaultConstruction(LanguageWriter w,DefaultConstructionStatement state){
			new TypeRef(state.var_type).WriteName(w);
			w.Write(" ");
			w.WriteDeclaration(state.var_name);
			w.Write("; ");
			w.WriteComment("// ����̃R���X�g���N�^");
			w.WriteLine();
		}
		private static void WriteDo(LanguageWriter w,IDoStatement state) {
			w.WriteKeyword("do");
			w.PushScope();
			w.Write(" {");
			w.WriteLine();
			w.WriteIndent();
			if(state.Body!=null)WriteBlock(w,state.Body);
			w.WriteOutdent();
			w.Write("} ");
			w.PopScope();
			w.WriteKeyword("while");
			w.Write("(");
			if(state.Condition!=null){
				w.SkipWriteLine=true;
				ExpressionWriter.WriteExpression(w,state.Condition,false);
				w.SkipWriteLine=false;
			}
			w.Write(");");
			w.WriteLine();
		}
		private static void WriteExpression(LanguageWriter w,IExpressionStatement state){
			w.WriteExpression(state.Expression);
			
			if(w.SkipWriteLine)return;

			w.Write(";");
			w.WriteLine();
		}
		private static void WriteFixed(LanguageWriter w,IFixedStatement state) {
			w.Write("{");
			w.WriteIndent();
			w.WriteLine();

			w.scope.RegisterVariable(state.Variable,false);
			w.WriteKeyword("pin_ptr");
			w.Write("<");
			new TypeRef(((IPointerType)state.Variable.VariableType).ElementType).WriteNameWithRef(w);
			w.Write("> ");
			w.WriteDeclaration(state.Variable.Name);
			w.Write(" = ");
			ExpressionWriter.WriteExpression(w,state.Expression,false);

			WriteBlock(w,state.Body);
			w.WriteOutdent();
			w.Write("}");
		}
		private static void WriteForEach(LanguageWriter w,IForEachStatement state) {
			w.PushScope();
			w.WriteKeyword("for each");
			w.Write(" (");
			ExpressionWriter.WriteVariableDeclaration(w,state.Variable);
			w.Write(" ");
			w.WriteKeyword("in");
			w.Write(" ");
			w.SkipWriteLine=true;
			ExpressionWriter.WriteExpression(w,state.Expression,false);
			w.SkipWriteLine=false;
			w.Write(") {");
			w.WriteLine();
			w.WriteIndent();
			if(state.Body!=null){
				WriteBlock(w,state.Body);
			}
			w.WriteOutdent();
			w.Write("}");
			w.WriteLine();
			w.PopScope();
		}

		private static void WriteFor(LanguageWriter w,IForStatement state) {
			w.PushScope();
			w.WriteKeyword("for");
			w.Write(" ");
			w.Write("(");
			if(state.Initializer!=null) {
				w.SkipWriteLine=true;
				WriteStatement(w,state.Initializer);
				w.SkipWriteLine=false;
				w.Write(" ");
			}
			w.Write("; ");
			if(state.Condition!=null){
				w.SkipWriteLine=true;
				ExpressionWriter.WriteExpression(w,state.Condition,false);
				w.SkipWriteLine=false;
			}
			w.Write("; ");
			if(state.Increment!=null) {
				w.SkipWriteLine=true;
				WriteStatement(w,state.Increment);
				w.SkipWriteLine=false;
			}
			w.Write(") {");
			w.WriteLine();
			w.WriteIndent();
			if(state.Body!=null){
				WriteStatement(w,state.Body);
			}
			w.WriteOutdent();
			w.Write("}");
			w.WriteLine();
			w.PopScope();
		}
		private static void WriteLabeled(LanguageWriter w,ILabeledStatement state) {
			w.__WriteLabel(state.Name);
			if(state.Statement!=null)
				WriteStatement(w,state.Statement);
		}
		private static int WriteLock_counter=0;
		private static void WriteLock(LanguageWriter w,ILockStatement statement) {
			w.Write("{");
			w.WriteIndent();
			w.WriteLine();

			Scope.VariableData data=new Scope.VariableData("<lock_statement>"+WriteLock_counter++.ToString(),true);
			data.disp_name="lock";
			w.scope[data.name]=data;

			// msclr::lock ��������
			w.WriteReference("msclr","",null);
			w.Write("::");
			w.WriteReference("lock","#include <msclr/lock.h> �Ŏg�p���ĉ�����",null);
			w.Write(" ");
			w.WriteDeclaration(data.disp_name);
			w.Write("(");
			ExpressionWriter.WriteExpression(w,statement.Expression,false);
			w.Write(");");
			w.WriteLine();

			// ���g������
			if(statement.Body!=null) {
				WriteBlock(w,statement.Body);
			}

			w.WriteOutdent();
			w.Write("}");
			w.WriteLine();
		}
		private static void WriteMemoryCopy(LanguageWriter w,IMemoryCopyStatement state) {
			w.Write("::");
			w.WriteReference("memcpy","Crt �֐� #include <memory.h>",null);
			w.Write("(");
			ExpressionWriter.WriteExpression(w,state.Destination,false);
			w.Write(", ");
			ExpressionWriter.WriteExpression(w,state.Source,false);
			w.Write(", ");
			ExpressionWriter.WriteExpression(w,state.Length,false);
			w.Write(");");
			w.WriteLine();
		}
		private static void WriteMemoryInitialize(LanguageWriter w,IMemoryInitializeStatement state) {
			w.Write("::");
			w.WriteReference("memset","Crt �֐� #include <memory.h>",null);
			w.Write("(");
			ExpressionWriter.WriteExpression(w,state.Offset,false);
			w.Write(", ");
			ExpressionWriter.WriteExpression(w,state.Value,false);
			w.Write(", ");
			ExpressionWriter.WriteExpression(w,state.Length,false);
			w.Write(");");
			w.WriteLine();
		}
		private static void WriteMethodReturn(LanguageWriter w,IMethodReturnStatement state) {
			w.WriteKeyword("return");
			if(state.Expression!=null) {
				w.Write(" ");
				ExpressionWriter.WriteExpression(w,state.Expression,false);
			}
			w.Write(";");
			w.WriteLine();
		}
		private static void WriteSwitch(LanguageWriter w,ISwitchStatement state) {
			w.WriteKeyword("switch");
			w.Write(" (");
			ExpressionWriter.WriteExpression(w,state.Expression,false);
			w.Write(") {");
			w.WriteLine();
			w.WriteIndent();
			foreach(ISwitchCase _case in state.Cases) {
				IConditionCase case1=_case as IConditionCase;
				if(case1!=null) {
					WriteSwitch_case(w,case1.Condition);
					w.WriteIndent();
					if(case1.Body!=null) {
						WriteStatement(w,case1.Body);
					}
					w.WriteOutdent();
				}
				IDefaultCase case2=_case as IDefaultCase;
				if(case2!=null) {
					w.WriteKeyword("default");
					w.Write(":");
					w.WriteLine();
					w.WriteIndent();
					if(case2.Body!=null) {
						WriteStatement(w,case2.Body);
					}
					w.WriteOutdent();
					//this.Write("}");
					//this.WriteLine();
				}
			}
			w.WriteOutdent();
			w.Write("}");
			w.WriteLine();
		}
		private static void WriteSwitch_case(LanguageWriter w,IExpression con_case) {
			IBinaryExpression exp_bi=con_case as IBinaryExpression;
			if(exp_bi!=null){
				if(exp_bi.Operator==BinaryOperator.BooleanOr) {
					WriteSwitch_case(w,exp_bi.Left);
					WriteSwitch_case(w,exp_bi.Right);
					return;
				}
			}

			w.WriteKeyword("case");
			w.Write(" ");
			ExpressionWriter.WriteExpression(w,con_case,false);
			w.Write(":");
			w.WriteLine();
		}
		private static void WriteThrowException(LanguageWriter w,IThrowExceptionStatement state) {
			w.WriteKeyword("throw");
			if(state.Expression!=null) {
				w.Write(" ");
				ExpressionWriter.WriteExpression(w,state.Expression,false);
			}
			w.Write(";");
			w.WriteLine();
		}
		private static void WriteTryCatchFinally(LanguageWriter w,ITryCatchFinallyStatement state) {
#if FUNC_TRY
			int skipTryCount=w.SkipTryCount;
			w.SkipTryCount=0;
#endif

#if FALSE
			if(state.Try!=null) {
				foreach(ICatchClause current in state.CatchClauses) {
					if(current.Body.Statements.Count!=0)goto write_try;
				}

				if(state.Finally.Statements.Count!=0)goto write_try;
				if(state.Fault.Statements.Count!=0)goto write_try;

				// ���g�������Ȃ��ꍇ
				this.WriteBlockStatement(state.Try);
				if(skipTryCount!=0) {
					this.WriteOutdent();
					this.Write("}");
					this.WriteLine();
				}
				return;
			}
		write_try:
#endif

			WriteTryCatchFinally_try(w,state.Try); //,skipTryCount

			//
			// catch ��
			//
			foreach(ICatchClause clause in state.CatchClauses) {
				WriteTryCatchFinally_catch(w,clause);
			}

			//
			// fault ��
			//
			if(state.Fault!=null&&state.Fault.Statements.Count>0) {
				w.Write(" ");
				w.WriteKeyword("fault");
				w.Write(" {");
				w.WriteLine();
				w.WriteIndent();
				WriteBlock(w,state.Fault);
				w.WriteOutdent();
				w.Write("}");
			}

			//
			// finally ��
			//
			if(state.Finally!=null&&state.Finally.Statements.Count>0) {
				w.Write(" ");
				w.WriteKeyword("finally");
				w.Write(" {");
				w.WriteLine();
				w.WriteIndent();
				WriteBlock(w,state.Finally);
				w.WriteOutdent();
				w.Write("}");
			}

			w.WriteLine();
		}
		private static void WriteTryCatchFinally_try(LanguageWriter w,IBlockStatement _try){ //,int skipCount
#if FUNC_TRY
			if(skipCount==0) {
#endif
				w.WriteKeyword("try");
				w.Write(" {");
				w.WriteLine();
				w.WriteIndent();
#if FUNC_TRY
			}
#endif
			//
			//	try �̒��g
			//
			if(_try!=null){
#if EXTRA_TEMP
				IStatementCollection statementCollection=_try.Statements;
				int i=0;
				foreach(IStatement state in _try.Statements){
#warning try: ���̃X�L�b�v������̂�?
					// ���R���X�g���N�^����  ��S�� delegation ��������B
					// �Edelegation �̌����́A�ŏ��� delegation ���K�p�o���Ȃ������o�����ɏI������
					//   �A���A���̍ۂɁA���[�J���ϐ� T var=xxx �͖������Ă���?
					// �E�Ō�� delegation �� skipCount
					if(i<skipCount){
						i++;

						IExpressionStatement state_exp=state as IExpressionStatement;
						if(state_exp==null)goto write;

						IAssignExpression exp_assign=state_exp.Expression as IAssignExpression;
						if(exp_assign!=null) {
							// this �ȊO�̕��� field ���X�L�b�v����Ă��܂�...
							if(exp_assign.Target is IFieldReferenceExpression)continue;
							goto write;
						}

						IMethodInvokeExpression exp_inv=state_exp.Expression as IMethodInvokeExpression;
						if(exp_inv!=null) {
							IMethodReferenceExpression method=exp_inv.Method as IMethodReferenceExpression;
							if(method!=null&&method.Target is IBaseReferenceExpression)continue;
							goto write;
						}
					}
				write:
					WriteStatement(w,state);
				}
#else
				WriteBlock(w,_try);
#endif
			}
			w.WriteOutdent();
			w.Write("}");
		}
		private static void WriteTryCatchFinally_catch(LanguageWriter w,ICatchClause clause){
			w.Write(" ");
			w.WriteKeyword("catch");

			// ���ʂȏꍇ catch(...) �����o
			// �� clauseBody ������
			//    clauseBody �� StatementAnalyze �̎��_�ŉ��ς���l�ɂ���
			bool catchAll=clause.Condition is StatementAnalyze.IsCatchAll;
			IStatementCollection clause_body=clause.Body==null?null:clause.Body.Statements;

			TypeRef var_type=new TypeRef(clause.Variable.VariableType);
			/*
			if(var_type.IsObject&&clause.Condition!=null) {
				ISnippetExpression iSnippent=clause.Condition as ISnippetExpression;
				if(iSnippent!=null&&iSnippent.Value=="?"&&StatementAnalyze.IsCatchAllClause(ref clause_body)){
					catchAll=true;
				}
			}
			//*/

			if(catchAll) {
				w.Write(" (...)");
			} else {
				if(clause.Variable.Name.Length!=0||!var_type.IsObject){
					w.scope.RegisterVariable(clause.Variable,false);
					w.Write(" (");
					var_type.WriteNameWithRef(w);
					w.Write(" ");
					w.WriteDeclaration(clause.Variable.Name);
					w.Write(")");
				}

				if(clause.Condition!=null) {
					w.Write(" ");
					w.WriteKeyword("when");
					w.Write(" ");
					w.Write("(");
					w.WriteExpression(clause.Condition);
					w.Write(")");
				}
			}

			w.Write(" {");
			w.WriteLine();
			w.WriteIndent();

			if(clause_body!=null) {
				for(int num=0;num<clause_body.Count;num++) {
					IStatement statement3=clause_body[num];

#if EXTRA_TEMP
#warning catch: "�R���X�g���N�^�̒��ŁA�Ō�̈�s�ŁAthrow" �̏ꍇ�͏������܂Ȃ�?
					// ���V�́A���炭�A constructor �� function-try-statement ��K�p�������̘b�ł���
					// ����Ė�������
					if(w.SomeConstructor&&num+1>=clause_body.Count&&statement3 is IThrowExceptionStatement)
						break;
#endif

					WriteStatement(w,statement3);
				}
			}

			w.WriteOutdent();
			w.Write("}");
		}
		private static void WriteRemoveEvent(LanguageWriter w,IRemoveEventStatement state) {
			ExpressionWriter.WriteEventReference(w,state.Event);
			w.Write(" -= ");
			ExpressionWriter.WriteExpression(w,state.Listener,false);
			w.Write(";");
			w.WriteLine();
		}
		private static void WriteWhile(LanguageWriter w,IWhileStatement state) {
			w.WriteKeyword("while");
			w.Write(" ");
			w.Write("(");
			if(state.Condition!=null) {
				w.SkipWriteLine=true;
				ExpressionWriter.WriteExpression(w,state.Condition,false);
				w.SkipWriteLine=false;
			}
			w.Write(") {");
			w.WriteLine();
			w.WriteIndent();
			if(state.Body!=null){
				WriteStatement(w,state.Body);
			}
			w.WriteOutdent();
			w.Write("}");
			w.WriteLine();
		}
	}

	[System.Serializable]
	public class InterfaceNotImplementedException:System.NotImplementedException{
		/// <summary>
		/// ���҂��Ă���C���^�[�t�F�C�X���A����̃I�u�W�F�N�g���������Ă��Ȃ������ꍇ�ɔ��������O�ł��B
		/// </summary>
		/// <param name="msg">��O�̋N�������󋵂Ȃǂ�������镶������w�肵�܂��B</param>
		/// <param name="type">���҂��Ă���C���^�[�t�F�C�X�̌^���w�肵�܂��B</param>
		/// <param name="target">�C���^�[�t�F�C�X�������Ă��鎖�����҂��Ă����I�u�W�F�N�g���w�肵�܂��B</param>
		public InterfaceNotImplementedException(string msg,System.Type interfaceType,object target):base(msg){
			this.required=interfaceType;
			this.target=target;
		}
		public InterfaceNotImplementedException(System.Type interfaceType,object target)
			:this("����I�u�W�F�N�g�����҂��� interface ���������Ă��܂���",interfaceType,target){}
		public InterfaceNotImplementedException(string msg,System.Type interfaceType,object target,System.Exception innerException):base(msg,innerException){
			this.required=interfaceType;
			this.target=target;
		}
		public InterfaceNotImplementedException(System.Type interfaceType,object target,System.Exception innerException)
			:this("����I�u�W�F�N�g�����҂��� interface ���������Ă��܂���",interfaceType,target,innerException){}

		public System.Type required;
		public object target;

		/// <summary>
		/// �C���^�[�t�F�C�X�����̏󋵂�������镶������擾���܂��B
		/// </summary>
		public override string Message {
			get{
				System.Text.StringBuilder b=new System.Text.StringBuilder();
				b.Append("�C���^�[�t�F�C�X������: ");
				b.AppendLine(base.Message);

				b.Append("<expecting interface> ");
				b.AppendLine(required.ToString());

				b.AppendLine("<implemented interface>");
				if(target==null){
					b.Append("-- null --");
				}else{
					System.Type[] ifaces=target.GetType().GetInterfaces();
					if(ifaces.Length==0){
						b.Append("-- no interfaces --");
					}else for(int i=0;i<ifaces.Length;i++){
						b.AppendLine(ifaces[i].ToString());
					}
				}

				return b.ToString();
			}
		}
	}
}