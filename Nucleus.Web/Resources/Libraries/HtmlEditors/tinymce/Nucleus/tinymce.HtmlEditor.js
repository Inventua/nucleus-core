(function ($)
{
  jQuery.fn.HtmlEditor = function (conf)
  {
    
    // For every element passed to the plug-in
    return this.each(function (index, value)
    {
      if (tinymce.activeEditor !== null && !document.body.contains(tinymce.activeEditor.getElement()))
      {
        tinymce.EditorManager.remove();
      }

      var htmlEditor = tinymce.init({
        target: value,
        height: '100%',
        skin: 'tinymce-5',
        plugins: 'code link lists',
        menubar: false,
        toolbar: 'code | undo redo | blocks | bold italic | link pages unlink | images | alignleft aligncenter alignright alignjustify | bullist numlist | hr | removeformat',
        statusbar: false,
        external_plugins: {
          pages: '../Nucleus/tinymce.pages.min.js',
          images: '../Nucleus/tinymce.images.min.js'
        }
      });

    });
  }
})(jQuery);