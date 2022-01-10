// jQuery.fn.HtmlEditor = jQuery.fn.trumbowyg;
// https://alex-d.github.io/Trumbowyg/documentation/#disabled
(function ($)
{
  jQuery.fn.HtmlEditor = function (conf)
  {    
    // For every element passed to the plug-in
    return this.each(function (index, value)
    {
      var textarea = jQuery(value).trumbowyg({
        btns: [
          ['viewHTML'],
          ['undo', 'redo'], // Only supported in Blink browsers
          ['formatting'],
          ['strong', 'em'], // ['strong', 'em', 'del'],
          //['superscript', 'subscript'],
          ['link'],
          ['insertImage'],
          ['justifyLeft', 'justifyCenter', 'justifyRight', 'justifyFull'],
          ['unorderedList', 'orderedList'],
          ['horizontalRule'],
          ['removeformat'],
          ['fullscreen']
        ],
        semantic: true,
        removeformatPasted: true,
        tagsToRemove: ['script', 'link'],
        imageWidthModalEdit: true
      });


      // this is a workaround.  contenteditable divs don't always get focus on click in Edge, especially in popups
      textarea.siblings('.trumbowyg-editor').on('click', function ()
      {
        jQuery(this).focus(); return false;
      });
    });
  }
})(jQuery);