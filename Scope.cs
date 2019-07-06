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
		/// ������̃X�R�[�v�ɓ���܂��B
		/// </summary>
		/// <param name="current">
		/// ���݂̃X�R�[�v���w�肵�܂��B
		/// null �ł��\�ł��B���̏ꍇ�ɂ͈�ԊO���̃X�R�[�v�Ƃ��������ɂȂ�܂��B
		/// �V���������̃X�R�[�v��Ԃ��܂��B
		/// </param>
		public static void Push(ref Scope current){
			current=new Scope(current);
		}
		/// <summary>
		/// ��O���̃X�R�[�v�ɓ���܂��B
		/// </summary>
		/// <param name="current">���݂̃X�R�[�v���w�肵�܂��B
		/// ��O���̃X�R�[�v��Ԃ��܂��B��ԊO���̃X�R�[�v�ɂ����ꍇ�ɂ� null ��Ԃ��܂��B</param>
		public static void Pop(ref Scope current){
			if(current==null)throw new System.ArgumentNullException("null �X�R�[�v�̊O���ɂ͏o���܂���B");
			current=current.parent;
		}
		/// <summary>
		/// ���݂̕ϐ��̏���S�ď����܂��B
		/// </summary>
		public void Clear(){
			this.vars.Clear();
		}
		/// <summary>
		/// �w�肵�����O�����Q�Ɖ\�ȕϐ����擾���܂��B
		/// </summary>
		/// <param name="name">�擾����ϐ������w�肵�܂��B</param>
		/// <returns>
		/// �w�肵���ϐ��ɑΉ��������Ԃ��܂��B
		/// �w�肵���ϐ������݂��Ă��Ȃ��ꍇ�ɂ͊���l (VariableData.IsNull �Ŋm�F�\) ��Ԃ��܂��B
		/// </returns>
		public VariableData this[string name]{
			get{
				foreach(VariableData var in vars){
					if(var.name==name)return var;
				}
				if(parent!=null){
					return parent[name];
				}
				throw new System.ArgumentOutOfRangeException("�w�肵�����O�̕ϐ��͓o�^����Ă��܂���B");
			}
			set{
				if(ContainsVariable(name))
					throw new System.InvalidOperationException("[�d������錾�ł�]\r\n�w�肵�����O�̕ϐ��͊��ɓo�^����Ă��܂��B");

				// �Փ˂��Ȃ��l�� displayName ��I��
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
		/// �w�肵�����O�̕ϐ������ɓo�^����Ă��邩�ۂ����m�F���܂��B
		/// </summary>
		/// <param name="name">�m�F����ϐ��̖��O���w�肵�܂��B</param>
		/// <returns>�w�肵�����O�����ϐ������ɃX�R�[�v���ɑ��݂����ꍇ�ɂ� true ��Ԃ��܂��B
		/// ����ȊO�̏ꍇ�ɂ� false ��Ԃ��܂��B</returns>
		public bool ContainsVariable(string name){
			foreach(VariableData var in vars)
				if(var.name==name)return true;
			return parent!=null&&parent.ContainsVariable(name);
		}
		/// <summary>
		/// �w�肵�����O�ŕ\�������ϐ������ɓo�^����Ă��邩�ۂ����m�F���܂��B
		/// </summary>
		/// <param name="displayName">�\���Ɏg�p�����ϐ������w�肵�܂��B</param>
		/// <returns>�w�肵�����O�ŕ\�������ϐ������ɓo�^����Ă����ꍇ�� true ��Ԃ��܂��B
		/// ����ȊO�̏ꍇ�ɂ� false ��Ԃ��܂��B</returns>
		public bool ContainsName(string displayName){
			foreach(VariableData var in vars)
				if(var.disp_name==displayName) return true;
			return parent!=null&&parent.ContainsName(displayName);
		}
		//===========================================================
		//		�ϐ����
		//===========================================================
		public class VariableData{
			/// <summary>
			/// ���ۂ� (Reflector �ɂ��) �ϐ�����ێ����܂��B
			/// </summary>
			public string name;
			/// <summary>
			/// �\���Ɏg�p���閼�O��ێ����܂��B
			/// ���̒l�́A�ϐ������\���ł������ꍇ�Ȃǂɂ͎��ۂ̕ϐ����Ƃ͈قȂ�l�����\��������܂��B
			/// </summary>
			public string disp_name;
			/// <summary>
			/// ref class �̃X�R�[�v���L�@�ネ�[�J���ł��鎖�������܂��B
			/// �����A�X�R�[�v�̖��[�� Dispose ���Ă΂��l�ȏꍇ���w���܂��B
			/// </summary>
			public bool local_scope;

			public VariableData(){
				this.name=null;
				this.local_scope=false;
			}
			public VariableData(string name,bool isLocalScope){
				this.disp_name=this.name=name??"<�s���Ȏ��ʎq>";
				this.local_scope=isLocalScope;
			}

			public bool IsNull{
				get{return this.name==null;}
			}
		}
	}
}