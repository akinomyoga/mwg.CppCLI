using Gen=System.Collections.Generic;
using Reflector.CodeModel;

namespace mwg.Reflector.CppCli{

	public class Scope{
		private Scope parent;
		//private Gen::Dictionary<string,VariableData> vars
		//	=new System.Collections.Generic.Dictionary<string,VariableData>();
		private Gen::List<VariableData> vars=new System.Collections.Generic.List<VariableData>();

		private Scope(Scope parent){
			this.parent=parent;
		}

		/// <summary>
		/// 一つ内側のスコープに入ります。
		/// </summary>
		/// <param name="current">
		/// 現在のスコープを指定します。
		/// null でも可能です。その場合には一番外側のスコープという扱いになります。
		/// 新しい内側のスコープを返します。
		/// </param>
		public static void Push(ref Scope current){
			current=new Scope(current);
		}
		/// <summary>
		/// 一つ外側のスコープに入ります。
		/// </summary>
		/// <param name="current">現在のスコープを指定します。
		/// 一つ外側のスコープを返します。一番外側のスコープにいた場合には null を返します。</param>
		public static void Pop(ref Scope current){
			if(current==null)throw new System.ArgumentNullException("null スコープの外側には出られません。");
			current=current.parent;
		}
		/// <summary>
		/// 現在の変数の情報を全て消します。
		/// </summary>
		public void Clear(){
			this.vars.Clear();
		}
		/// <summary>
		/// 指定した名前を持つ参照可能な変数を取得します。
		/// </summary>
		/// <param name="name">取得する変数名を指定します。</param>
		/// <returns>
		/// 指定した変数に対応する情報を返します。
		/// 指定した変数が存在していない場合には既定値 (VariableData.IsNull で確認可能) を返します。
		/// </returns>
		public VariableData this[string name]{
			get{
				foreach(VariableData var in vars){
					if(var.name==name)return var;
				}
				if(parent!=null){
					return parent[name];
				}
				throw new System.ArgumentOutOfRangeException("指定した名前の変数は登録されていません。");
			}
			set{
				if(ContainsVariable(name))
					throw new System.InvalidOperationException("[重複する宣言です]\r\n指定した名前の変数は既に登録されています。");

				// 衝突しない様に displayName を選択
				if(ContainsName(value.disp_name)){
					string orig_disp=value.disp_name;
					value.disp_name="loc_"+orig_disp;
					int i=0;
					while(ContainsName(value.disp_name)){
						value.disp_name=string.Format("loc{0}_{1}",i++,orig_disp);
					}
				}

				vars.Add(value);
			}
		}
		public VariableData RegisterVariable(IVariableDeclaration decl,bool local_scope){
			return this[decl.Name]=new VariableData(decl.Name,local_scope);
		}
		public VariableData RegisterVariable(string var_name,bool local_scope){
			return this[var_name]=new VariableData(var_name,local_scope);
		}
		/// <summary>
		/// 指定した名前の変数が既に登録されているか否かを確認します。
		/// </summary>
		/// <param name="name">確認する変数の名前を指定します。</param>
		/// <returns>指定した名前を持つ変数が既にスコープ内に存在した場合には true を返します。
		/// それ以外の場合には false を返します。</returns>
		public bool ContainsVariable(string name){
			foreach(VariableData var in vars)
				if(var.name==name)return true;
			return parent!=null&&parent.ContainsVariable(name);
		}
		/// <summary>
		/// 指定した名前で表示される変数が既に登録されているか否かを確認します。
		/// </summary>
		/// <param name="displayName">表示に使用される変数名を指定します。</param>
		/// <returns>指定した名前で表示される変数が既に登録されていた場合に true を返します。
		/// それ以外の場合には false を返します。</returns>
		public bool ContainsName(string displayName){
			foreach(VariableData var in vars)
				if(var.disp_name==displayName) return true;
			return parent!=null&&parent.ContainsName(displayName);
		}
		//===========================================================
		//		変数情報
		//===========================================================
		public class VariableData{
			/// <summary>
			/// 実際の (Reflector による) 変数名を保持します。
			/// </summary>
			public string name;
			/// <summary>
			/// 表示に使用する名前を保持します。
			/// この値は、変数名が予約語であった場合などには実際の変数名とは異なる値を取る可能性があります。
			/// </summary>
			public string disp_name;
			/// <summary>
			/// ref class のスコープが記法上ローカルである事を示します。
			/// 則ち、スコープの末端で Dispose が呼ばれる様な場合を指します。
			/// </summary>
			public bool local_scope;

			public VariableData(){
				this.name=null;
				this.local_scope=false;
			}
			public VariableData(string name,bool isLocalScope){
				this.disp_name=this.name=name??"<不明な識別子>";
				this.local_scope=isLocalScope;
			}

			public bool IsNull{
				get{return this.name==null;}
			}
		}
	}
}