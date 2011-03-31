namespace Reflector.Languages {
	using Reflector;
	using System;

	internal class CppCliLanguagePackage:IPackage {
		private CppCliLanguage cppCliLanguage;
		private ILanguageManager languageManager;

		public virtual void Load(IServiceProvider serviceProvider) {
			this.languageManager=(ILanguageManager)serviceProvider.GetService(typeof(ILanguageManager));
			this.cppCliLanguage=new CppCliLanguage();
			this.languageManager.RegisterLanguage(this.cppCliLanguage);
		}

		public virtual void Unload() {
			this.languageManager.UnregisterLanguage(this.cppCliLanguage);
		}
	}
}

