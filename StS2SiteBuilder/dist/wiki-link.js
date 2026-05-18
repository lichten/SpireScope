(function () {
  var jaTitle = document.querySelector('.card-title-ja');
  if (!jaTitle) return;
  var jaName = jaTitle.textContent.trim();
  if (!jaName) return;

  var link = document.createElement('a');
  link.href = 'https://wikiwiki.jp/sts2/' + encodeURIComponent(jaName);
  link.target = '_blank';
  link.rel = 'noopener noreferrer';
  link.className = 'wiki-link';
  link.textContent = 'wikiwiki.jp ↗';

  var badges = document.querySelector('.card-badges');
  if (badges) badges.insertAdjacentElement('afterend', link);
})();
