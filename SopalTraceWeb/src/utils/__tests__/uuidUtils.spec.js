import { describe, it, expect, vi, afterEach } from 'vitest';
import { genererUid } from '../uuidUtils';

const UUID_V4 = /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/;

describe('genererUid', () => {
  afterEach(() => vi.unstubAllGlobals());

  it('renvoie un UUID v4', () => {
    expect(genererUid()).toMatch(UUID_V4);
  });

  it('fonctionne sans crypto.randomUUID (site en HTTP)', () => {
    const vrai = globalThis.crypto;
    vi.stubGlobal('crypto', { getRandomValues: (t) => vrai.getRandomValues(t) });
    const a = genererUid();
    const b = genererUid();
    expect(a).toMatch(UUID_V4);
    expect(a).not.toBe(b);
  });
});
