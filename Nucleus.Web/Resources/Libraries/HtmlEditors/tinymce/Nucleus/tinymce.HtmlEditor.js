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
        plugins = 'code link lists table'
        toolbar = 'code | undo redo | blocks | table | bold italic strikethrough | link pages unlink | images | alignleft aligncenter alignright alignjustify | bullist numlist | hr | removeformat';
        external_plugins = {
          pages: '../Nucleus/tinymce.pages.min.js',
          images: '../Nucleus/tinymce.images.min.js'
        };
      }
      else
      {
        plugins = 'link lists'
        toolbar = 'code | undo redo | blocks | bold italic strikethrough | alignleft aligncenter alignright alignjustify | bullist numlist | hr | removeformat';
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
        setup: function (editor)
        {
          editor.on('GetContent', function (e) { jQuery(editor.targetElm).val(e.content); })
          editor.on('change', function (e) { jQuery(editor.targetElm).val(editor.getBody().innerHTML); })
        },
        paste_block_drop: false,
        paste_data_images: true,
        paste_as_text: true,
        external_plugins: external_plugins,
        block_formats: 'Paragraph=p; Heading 1=h1; Heading 2=h2; Heading 3=h3; Heading 4=h4; Heading 5=h5; Heading 6=h6; Preformatted=pre; Code=code'
      });
    });
  }
})(jQuery);