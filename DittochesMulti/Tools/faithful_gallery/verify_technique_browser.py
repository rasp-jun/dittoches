"""Exercise technique effects with real rendered poses and playback controls."""
import math


def verify_techniques(page, entry, output):
    ident=entry['id'];spec=entry['techniques'];results=[]
    assert spec['source'].startswith('https://digimon.net/reference_en/detail.php?directory_name=')
    assert page.locator('#technique-card').is_visible()
    assert page.locator('#technique-source').get_attribute('href')==spec['source']
    def seek(u):
        frame=page.evaluate('faithfulGallery.state.frames')
        page.evaluate('(u)=>faithfulGallery.seekMotion(u*faithfulGallery.state.animationDuration)',u)
        page.wait_for_function('(n)=>faithfulGallery.state.frames>n',arg=frame)
        page.wait_for_function('(u)=>Math.abs(faithfulGallery.state.animationTime/faithfulGallery.state.animationDuration-u)<.001',arg=u)
        return page.evaluate('faithfulGallery.state.techniqueEffect')
    for mode in ('Attack','Skill'):
        page.locator('[data-motion="'+mode+'"]').click()
        assert page.locator('#technique-'+mode.lower()+' strong').inner_text()==spec[mode]['name']
        assert page.locator('#technique-'+mode.lower()).get_attribute('aria-current')=='true'
        start=seek(0);assert not start['active'],(ident,mode,'effect at neutral entry')
        charge=seek(.30)
        page.screenshot(path=str(output/(ident+'-'+mode+'-charge.png')))
        strike=seek(.67 if mode=='Skill' else .51)
        assert strike['active'] and strike['particles']>0,(ident,mode,'missing release effect',strike)
        assert strike['effect']==spec[mode]['effect']
        assert all(math.isfinite(v) for v in strike['emitter'])
        page.screenshot(path=str(output/(ident+'-'+mode+'-release.png')))
        page.locator('#technique-effects').uncheck()
        page.wait_for_function('!faithfulGallery.state.techniqueEffect.active')
        page.locator('#technique-effects').check()
        page.wait_for_function('faithfulGallery.state.techniqueEffect.active')
        assert not seek(.99)['active'],(ident,mode,'effect remained after recovery')
        results.append(dict(clip=mode,name=spec[mode]['name'],charge=charge,release=strike,source=spec['source']))
    page.locator('[data-motion="Idle"]').click()
    page.wait_for_function('!faithfulGallery.state.techniqueEffect.active')
    print('TECHNIQUE BROWSER PASS',ident,flush=True)
    return results
