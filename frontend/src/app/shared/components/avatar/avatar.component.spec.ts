import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AvatarComponent } from './avatar.component';
import { PhotoService } from '../../../core/services/photo.service';

describe('AvatarComponent', () => {
  let component: AvatarComponent;
  let fixture: ComponentFixture<AvatarComponent>;
  let photoServiceMock: jasmine.SpyObj<PhotoService>;

  beforeEach(async () => {
    photoServiceMock = jasmine.createSpyObj('PhotoService', ['load']);
    photoServiceMock.load.and.returnValue(of('blob:http://localhost/test-photo'));

    await TestBed.configureTestingModule({
      imports: [AvatarComponent],
      providers: [
        { provide: PhotoService, useValue: photoServiceMock }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AvatarComponent);
    component = fixture.componentInstance;
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should render initials when hasPhoto is false', () => {
    component.name = 'John Doe';
    component.hasPhoto = false;
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(component.computedInitials).toBe('JD');
    expect(compiled.querySelector('span')?.textContent?.trim()).toBe('JD');
    expect(compiled.querySelector('img')).toBeNull();
  });

  it('should compute two-letter initials for single-word names', () => {
    component.name = 'Abel';
    component.hasPhoto = false;
    fixture.detectChanges();

    expect(component.computedInitials).toBe('AB');
  });

  it('should fallback to question mark when name is empty', () => {
    component.name = '';
    component.hasPhoto = false;
    fixture.detectChanges();

    expect(component.computedInitials).toBe('?');
  });

  it('should prioritize explicit initials input', () => {
    component.name = 'John Doe';
    component.initials = 'XX';
    fixture.detectChanges();

    expect(component.computedInitials).toBe('XX');
  });

  it('should render photo image when hasPhoto is true and photo load succeeds', () => {
    component.kind = 'children';
    component.id = '123';
    component.name = 'Jane Doe';
    component.hasPhoto = true;
    fixture.detectChanges();

    expect(photoServiceMock.load).toHaveBeenCalledWith('children', '123', null);
    expect(component.photoUrl).toBe('blob:http://localhost/test-photo');

    const compiled = fixture.nativeElement as HTMLElement;
    const img = compiled.querySelector('img');
    expect(img).not.toBeNull();
    expect(img?.getAttribute('src')).toBe('blob:http://localhost/test-photo');
    expect(img?.getAttribute('alt')).toBe('Jane Doe');
  });

  it('should fallback to initials when image encounters load error', () => {
    component.kind = 'children';
    component.id = '123';
    component.name = 'Jane Doe';
    component.hasPhoto = true;
    fixture.detectChanges();

    expect(component.photoUrl).toBe('blob:http://localhost/test-photo');
    component.onImageError();
    fixture.detectChanges();

    expect(component.imageFailed).toBeTrue();
    expect(component.photoUrl).toBeNull();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('img')).toBeNull();
    expect(compiled.querySelector('span')?.textContent?.trim()).toBe('JD');
  });
});

