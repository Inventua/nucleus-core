(function ($)
{
  jQuery.fn.HtmlEditor = function (conf)
  {    
    // For every element passed to the plug-in
    return this.each(function (index, value)
    {
      var isAdminMode = conf == null || typeof (conf.isAdminMode) === 'undefined' ? false : conf.isAdminMode;
      var plugins;
      var toolbar;
      var external_plugins;

      if (isAdminMode)
      {
        plugins = 'code link lists'
        toolbar = 'code | undo redo | blocks | bold italic | link pages unlink | images | alignleft aligncenter alignright alignjustify | bullist numlist | hr | removeformat';
        external_plugins = {
          pages: '../Nucleus/tinymce.pages.min.js',
          images: '../Nucleus/tinymce.images.min.js'
        };
      }
      else
      {
        plugins = 'link lists'
        toolbar = 'code | undo redo | blocks | bold italic | link unlink | alignleft aligncenter alignright alignjustify | bullist numlist | hr | removeformat';
        external_plugins = {};
      }

      if (tinymce.activeEditor !== null && !document.body.contains(tinymce.activeEditor.getElement()))
      {
        tinymce.EditorManager.remove();
      }

      var htmlEditor = tinymce.init({
        target: value,
        document_base_url: document.baseURI,
        convert_urls: false,
        height: '100%',
        skin: 'tinymce-5',
        plugins: plugins,
        menubar: false,
        toolbar: toolbar,
        statusbar: false,
        paste_block_drop: false,
        paste_data_images: true,
        paste_as_text: true,
        external_plugins: external_plugins
      });

    });
  }
})(jQuery);