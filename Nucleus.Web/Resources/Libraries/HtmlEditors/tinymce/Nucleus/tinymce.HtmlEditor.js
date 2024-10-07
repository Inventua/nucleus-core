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
      var styles;
      var formats;

      if (isAdminMode)
      {
        plugins = 'link lists table'
        toolbar = 'code_monaco | undo redo | styles | table | bold italic strikethrough | link pages unlink | images | alignleft aligncenter alignright alignjustify | bullist numlist | hr | removeformat';
        external_plugins = {
          pages: '../Nucleus/tinymce.pages.min.js',
          images: '../Nucleus/tinymce.images.min.js',
          code_monaco: '../Nucleus/tinymce.code.min.js'
        };
      }
      else
      {
        plugins = 'link lists'
        toolbar = 'code | undo redo | styles | bold italic strikethrough | alignleft aligncenter alignright alignjustify | bullist numlist | hr | removeformat';
        external_plugins = {};
      }           

      // make the <mark> element available to styles
      formats =
      {
        mark: { inline: 'mark' }
      };

      // define which styles can be selected
      styles = [
        { title: 'Format', selector: '*' },     // this is a trick to make the toolbar label always say "format".  TinyMCE default bahavior is to display the format of the selection.
        { title: 'Heading 1', format: 'h1' },
        { title: 'Heading 2', format: 'h2' },
        { title: 'Heading 3', format: 'h3' },
        { title: 'Heading 4', format: 'h4' },
        { title: 'Heading 5', format: 'h5' },
        { title: 'Heading 6', format: 'h6' },
        { title: 'Pre', format: 'pre' },
        { title: 'Code', format: 'code' },
        { title: 'Mark', format: 'mark' }        
      ];

      // Prevent Bootstrap dialog from blocking focusin.  https://www.tiny.cloud/docs/integrations/bootstrap/
      document.addEventListener('focusin', (e) =>
      {
        if (e.target.closest(".tox-tinymce-aux, .moxman-window, .tam-assetmanager-root") !== null)
        {
          e.stopImmediatePropagation();
        }
      });

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
        contextmenu: false,
        setup: function (editor)
        {
          editor.on('GetContent', function (e) { jQuery(editor.targetElm).val(e.content); })
          editor.on('change', function (e) { jQuery(editor.targetElm).val(editor.getBody().innerHTML); })
        },
        paste_block_drop: false,
        paste_data_images: true,
        paste_as_text: true,
        external_plugins: external_plugins,
        style_formats: styles,
        formats: formats
      });
    });
  }
})(jQuery);