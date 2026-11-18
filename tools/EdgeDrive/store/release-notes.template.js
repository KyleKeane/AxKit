// Partner Center, an update's English listing (submissions/<id>/listings?languageid=4): "What's new in this version",
// the text area #releaseNotes (1500 characters), set as the page's form reads it: the native value setter, then input
// and change events, then a blur (docs/axKit/store-setup.md section 6). __TEXT__ is replaced by the text as a JSON
// string before the script is run (PowerShell: ConvertTo-Json -InputObject $text -Compress); then the page's Save,
// which is the second "Save" that EdgeDrive find lists when the text holds the word "saved".
(() => {
  const text = __TEXT__;
  const all = [];
  const walk = (root) => {
    for (const el of root.querySelectorAll('*')) {
      all.push(el);
      if (el.shadowRoot) walk(el.shadowRoot);
    }
  };
  walk(document);
  const fields = all.filter((el) => el.tagName === 'TEXTAREA' && el.id === 'releaseNotes');
  if (fields.length !== 1) return 'Not set: ' + fields.length + ' text areas are #releaseNotes.';
  const field = fields[0];
  if (field.maxLength > 0 && text.length > field.maxLength) return 'Not set: the text has ' + text.length + ' characters, the field takes ' + field.maxLength + '.';
  field.scrollIntoView({ block: 'center' });
  field.focus();
  const setter = Object.getOwnPropertyDescriptor(HTMLTextAreaElement.prototype, 'value').set;
  setter.call(field, text);
  field.dispatchEvent(new Event('input', { bubbles: true, composed: true }));
  field.dispatchEvent(new Event('change', { bubbles: true, composed: true }));
  field.dispatchEvent(new Event('blur', { bubbles: false, composed: true }));
  field.dispatchEvent(new FocusEvent('focusout', { bubbles: true, composed: true }));
  return 'Set: the field holds ' + field.value.length + ' characters, the text has ' + text.length + (field.value === text ? '; they are the same.' : '; THEY DIFFER.');
})()
