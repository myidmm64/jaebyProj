mergeInto(LibraryManager.library, {
  ImpactOrderSyncSave: function () {
    if (Module.impactSavePending) { Module.impactSaveAgain = true; return; }
    function sync() {
      Module.impactSavePending = true;
      FS.syncfs(false, function (error) {
        Module.impactSavePending = false;
        if (error) console.warn('Impact order: save persistence failed', error);
        if (Module.impactSaveAgain) {
          Module.impactSaveAgain = false;
          sync();
        }
      });
    }
    sync();
  }
});
