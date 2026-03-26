/** Normalise a display name or path segment into a URL-style slug */
export function toSlug(str: string): string {
  return str
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/(^-|-$)/g, '');
}

/** Strip common version suffixes so "aave-v3" base-matches "aave" */
export function baseSlug(slug: string): string {
  return slug.replace(/-v\d+.*$/, '');
}
